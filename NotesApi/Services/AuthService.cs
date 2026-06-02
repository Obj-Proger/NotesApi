using Microsoft.EntityFrameworkCore;
using NotesApi.Data;
using NotesApi.Dtos.Users;
using NotesApi.Dtos.Auth;
using NotesApi.Exceptions;
using NotesApi.Models;

namespace NotesApi.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(CreateUserDto createUserDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<bool> ValidateUserAsync(Guid userId);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        private const int RefreshTokenExpirationDaysDefault = 7;
        private const string RefreshTokenExpirationEnvironmentVariable = "JWT_REFRESH_EXPIRATION_DAYS";

        public AuthService(AppDbContext context, IJwtService jwtService, ILogger<AuthService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(CreateUserDto createUserDto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
                    throw new ConflictException("User with this email already exists.");

                if (await _context.Users.AnyAsync(u => u.Username == createUserDto.Username))
                    throw new ConflictException("Username is already taken.");

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

                var authResponse = GenerateAuthResponse(user);

                user.RefreshToken = authResponse.RefreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("New user registered: {Email}", user.Email);
                return authResponse;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
                throw new UnauthorizedException("Invalid email or password.");

            var authResponse = GenerateAuthResponse(user);

            user.RefreshToken = authResponse.RefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());
            await _context.SaveChangesAsync();

            _logger.LogInformation("User logged in: {Email}", user.Email);
            return authResponse;
        }

        public async Task<bool> ValidateUserAsync(Guid userId)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }

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

        private static string HashPassword(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password);

        private static bool VerifyPassword(string password, string hash) =>
            BCrypt.Net.BCrypt.Verify(password, hash);

        private static int GetAccessTokenExpirationMinutes() =>
            int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES") ?? "60");

        private static int GetRefreshTokenExpirationDays() =>
            int.Parse(
                Environment.GetEnvironmentVariable(RefreshTokenExpirationEnvironmentVariable)
                ?? RefreshTokenExpirationDaysDefault.ToString());
    }
}