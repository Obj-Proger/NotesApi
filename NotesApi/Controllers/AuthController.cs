using Microsoft.AspNetCore.Mvc;
using NotesApi.Dtos.Users;
using NotesApi.Dtos.Auth;
using NotesApi.Services;

namespace NotesApi.Controllers
{
    /// <summary>
    /// Handles user authentication operations including registration and login.
    /// Provides endpoints for account creation and credential verification.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user account with provided credentials.
        /// </summary>
        /// <param name="createUserDto">User registration data including email, password, and optional name fields.</param>
        /// <returns>
        /// HTTP 201 (Created) with access and refresh tokens on successful registration.
        /// HTTP 400 (Bad Request) if email/username already exists or validation fails.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Email and username must be unique. Password is hashed using BCrypt before storage.
        /// Both access and refresh tokens are generated and returned.
        /// </remarks>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                var result = await _authService.RegisterAsync(createUserDto);
                _logger.LogInformation("User registered: {Email}", createUserDto.Email);
                return StatusCode(201, result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Registration failed: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Authenticates a user with email and password credentials.
        /// </summary>
        /// <param name="loginDto">User credentials containing email and password.</param>
        /// <returns>
        /// HTTP 200 (OK) with access and refresh tokens on successful authentication.
        /// HTTP 401 (Unauthorized) if credentials are invalid.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Returns generic "Invalid email or password" message to prevent email enumeration attacks.
        /// New refresh token is generated on each successful login.
        /// </remarks>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);
                _logger.LogInformation("User logged in: {Email}", loginDto.Email);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Login failed: {Message}", ex.Message);
                return Unauthorized(new { error = "Invalid email or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}