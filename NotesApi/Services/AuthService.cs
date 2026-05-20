using Microsoft.EntityFrameworkCore;
using NotesApi.Data;
using NotesApi.Dtos.Users;
using NotesApi.Dtos.Auth;
using NotesApi.Models;

namespace NotesApi.Services
{
    /// <summary>
    /// Defines authentication service contract for user registration and login operations.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user account with email and password.
        /// </summary>
        /// <param name="createUserDto">User registration data.</param>
        /// <returns>Authentication response containing access and refresh tokens.</returns>
        Task<AuthResponseDto> RegisterAsync(CreateUserDto createUserDto);

        /// <summary>
        /// Authenticates user credentials and generates tokens.
        /// </summary>
        /// <param name="loginDto">User credentials (email and password).</param>
        /// <returns>Authentication response containing access and refresh tokens.</returns>
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// Verifies whether a user exists in the database.
        /// </summary>
        /// <param name="userId">The user identifier to validate.</param>
        /// <returns>True if user exists, false otherwise.</returns>
        Task<bool> ValidateUserAsync(Guid userId);
    }

    /// <summary>
    /// Implements user authentication including registration, login, and token generation.
    /// Uses BCrypt for password hashing and JWT for token-based authentication.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        private const int RefreshTokenExpirationDaysDefault = 7;
        private const string RefreshTokenExpirationEnvironmentVariable = "JWT_REFRESH_EXPIRATION_DAYS";

        public AuthService(
            AppDbContext context,
            IJwtService jwtService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user account and generates authentication tokens.
        /// Validates email and username uniqueness before creating the account.
        /// </summary>
        /// <param name="createUserDto">User registration data.</param>
        /// <returns>Authentication response with access and refresh tokens.</returns>
        /// <exception cref="InvalidOperationException">Thrown when email or username already exists.</exception>
        public async Task<AuthResponseDto> RegisterAsync(CreateUserDto createUserDto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verify email uniqueness to prevent duplicate accounts.
                if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
                    throw new InvalidOperationException("User with this email already exists");

                // Verify username uniqueness to prevent duplicate usernames.
                if (await _context.Users.AnyAsync(u => u.Username == createUserDto.Username))
                    throw new InvalidOperationException("Username is already taken");

                // Create new user entity with hashed password and normalized data.
                var user = new User
                {
                    Username = createUserDto.Username.Trim(),
                    Email = createUserDto.Email.Trim(),
                    FirstName = createUserDto.FirstName?.Trim() ?? "",
                    LastName = createUserDto.LastName?.Trim() ?? "",
                    PasswordHash = HashPassword(createUserDto.Password)
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Generate authentication tokens.
                var authResponse = GenerateAuthResponse(user);

                // Store refresh token for future token refresh operations.
                user.RefreshToken = authResponse.RefreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("New user registered: {Email}", user.Email);
                return authResponse;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Registration failed");
                throw;
            }
        }

        /// <summary>
        /// Authenticates user with email and password, and generates new tokens.
        /// </summary>
        /// <param name="loginDto">User credentials.</param>
        /// <returns>Authentication response with access and refresh tokens.</returns>
        /// <exception cref="InvalidOperationException">Thrown when credentials are invalid.</exception>
        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // Retrieve user by email from database.
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            // Validate credentials. Intentionally generic error message prevents email enumeration.
            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                throw new InvalidOperationException("Invalid email or password");

            _logger.LogInformation("User logged in: {Email}", user.Email);

            // Generate new tokens for authenticated user.
            var authResponse = GenerateAuthResponse(user);

            // Update refresh token with new value and expiration date.
            user.RefreshToken = authResponse.RefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());
            await _context.SaveChangesAsync();

            return authResponse;
        }

        /// <summary>
        /// Verifies whether a user exists in the database by ID.
        /// </summary>
        /// <param name="userId">The user identifier to validate.</param>
        /// <returns>True if user exists, false otherwise.</returns>
        public async Task<bool> ValidateUserAsync(Guid userId)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }

        /// <summary>
        /// Generates authentication response containing access and refresh tokens with user information.
        /// </summary>
        /// <param name="user">The authenticated user entity.</param>
        /// <returns>Authentication response DTO with tokens.</returns>
        private AuthResponseDto GenerateAuthResponse(User user)
        {
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expirationMinutes = GetAccessTokenExpirationMinutes();

            return new AuthResponseDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };
        }

        /// <summary>
        /// Hashes a plain text password using BCrypt algorithm.
        /// </summary>
        /// <param name="password">The plain text password to hash.</param>
        /// <returns>The hashed password.</returns>
        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verifies a plain text password against its BCrypt hash.
        /// </summary>
        /// <param name="password">The plain text password to verify.</param>
        /// <param name="hash">The BCrypt hash to compare against.</param>
        /// <returns>True if password matches hash, false otherwise.</returns>
        private static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        /// <summary>
        /// Retrieves access token expiration time from environment variables.
        /// </summary>
        /// <returns>Expiration time in minutes, defaults to 60 if not configured.</returns>
        private static int GetAccessTokenExpirationMinutes()
        {
            return int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES") ?? "60");
        }

        /// <summary>
        /// Retrieves refresh token expiration time from environment variables.
        /// </summary>
        /// <returns>Expiration time in days, defaults to 7 if not configured.</returns>
        private static int GetRefreshTokenExpirationDays()
        {
            return int.Parse(
                Environment.GetEnvironmentVariable(RefreshTokenExpirationEnvironmentVariable)
                ?? RefreshTokenExpirationDaysDefault.ToString());
        }
    }
}