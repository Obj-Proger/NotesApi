using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NotesApi.Models;

namespace NotesApi.Services
{
    /// <summary>
    /// Defines JWT token generation and validation operations.
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generates a signed JWT access token for the specified user.
        /// </summary>
        /// <param name="user">The user entity to create token for.</param>
        /// <returns>A signed JWT access token string.</returns>
        string GenerateAccessToken(User user);

        /// <summary>
        /// Generates a cryptographically secure refresh token.
        /// </summary>
        /// <returns>A base64-encoded refresh token string.</returns>
        string GenerateRefreshToken();

        /// <summary>
        /// Validates and extracts claims from an expired JWT token.
        /// Used for refresh token operations when access token is expired.
        /// </summary>
        /// <param name="token">The expired JWT token to validate.</param>
        /// <returns>ClaimsPrincipal with user claims if valid, null otherwise.</returns>
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }

    /// <summary>
    /// Implements JWT token generation and validation using HS256 algorithm.
    /// Tokens include user identification claims and are issued with fixed expiration.
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;

        private const string TokenIssuer = "NotesApi";
        private const string TokenAudience = "NotesApiUsers";
        private const string JwtSecretConfigurationKey = "JwtSettings:Secret";
        private const string JwtExpirationConfigurationKey = "JwtSettings:ExpirationMinutes";
        private const int AccessTokenExpirationMinutesDefault = 60;
        private const int RefreshTokenBytesLength = 32;

        public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Generates a signed JWT access token containing user claims.
        /// Token includes user ID, email, and username in the claims.
        /// </summary>
        /// <param name="user">The user to generate token for.</param>
        /// <returns>A valid JWT access token string.</returns>
        public string GenerateAccessToken(User user)
        {
            var secret = GetJwtSecret();
            var expirationMinutes = GetAccessTokenExpirationMinutes();

            // Create signing credentials using HMAC SHA256 algorithm.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Build claims that uniquely identify and describe the user.
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
            };

            // Create and sign the JWT token with specified parameters.
            var token = new JwtSecurityToken(
                issuer: TokenIssuer,
                audience: TokenAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogInformation("Access token generated for user {UserId}", user.Id);
            return tokenString;
        }

        /// <summary>
        /// Generates a cryptographically secure random refresh token.
        /// </summary>
        /// <returns>A base64-encoded 256-bit random token.</returns>
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[RefreshTokenBytesLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            var refreshToken = Convert.ToBase64String(randomNumber);
            _logger.LogInformation("Refresh token generated");
            return refreshToken;
        }

        /// <summary>
        /// Validates an expired JWT token and extracts its claims.
        /// Does not validate token lifetime, allowing expired tokens to be processed.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>ClaimsPrincipal containing user claims if valid, null on validation failure.</returns>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var secret = GetJwtSecret();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

                // Configure token validation parameters.
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = TokenAudience,
                    ValidateIssuer = true,
                    ValidIssuer = TokenIssuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateLifetime = false, // Skip lifetime validation for expired tokens.
                    ClockSkew = TimeSpan.Zero
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                // Verify the token uses the expected HS256 signing algorithm.
                if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("Invalid token algorithm");
                    return null;
                }

                _logger.LogInformation("Token validated successfully");
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return null;
            }
        }

        /// <summary>
        /// Retrieves JWT secret from environment variables or configuration.
        /// Checks environment variables first, then configuration file.
        /// </summary>
        /// <returns>The JWT secret string.</returns>
        /// <exception cref="InvalidOperationException">Thrown when secret is not configured.</exception>
        private string GetJwtSecret()
        {
            // Prioritize environment variable for production deployment flexibility.
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET");
            if (!string.IsNullOrEmpty(secret))
                return secret;

            // Fall back to configuration for development environments.
            secret = _configuration[JwtSecretConfigurationKey];
            if (!string.IsNullOrEmpty(secret))
                return secret;

            throw new InvalidOperationException(
                "JWT Secret is not configured. Set JWT_SECRET environment variable or JwtSettings:Secret in configuration.");
        }

        /// <summary>
        /// Retrieves access token expiration time from configuration.
        /// </summary>
        /// <returns>Expiration time in minutes, defaults to 60 if not configured.</returns>
        private int GetAccessTokenExpirationMinutes()
        {
            return int.Parse(_configuration[JwtExpirationConfigurationKey] ?? AccessTokenExpirationMinutesDefault.ToString());
        }
    }
}