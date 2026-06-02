using Microsoft.AspNetCore.Mvc;
using NotesApi.Dtos.Users;
using NotesApi.Dtos.Auth;
using NotesApi.Services;

namespace NotesApi.Controllers
{
    /// <summary>
    /// Handles user authentication: registration and login.
    /// Exceptions are handled globally by GlobalExceptionHandler.
    /// </summary>
    [Route("api/[controller]")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <returns>201 with tokens on success. 409 if email/username is taken.</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserDto createUserDto)
        {
            var result = await _authService.RegisterAsync(createUserDto);
            _logger.LogInformation("User registered: {Email}", createUserDto.Email);
            return StatusCode(201, result);
        }

        /// <summary>
        /// Authenticates a user and returns tokens.
        /// </summary>
        /// <returns>200 with tokens on success. 401 if credentials are invalid.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            _logger.LogInformation("User logged in: {Email}", loginDto.Email);
            return Ok(result);
        }
    }
}