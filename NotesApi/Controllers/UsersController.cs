using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesApi.Dtos.Users;
using NotesApi.Exceptions;
using NotesApi.Services;

namespace NotesApi.Controllers
{
    /// <summary>
    /// Manages user profile operations for authenticated users.
    /// All exceptions bubble up to GlobalExceptionHandler.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ApiControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>Gets a user's public profile. Returns 404 if not found.</summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(Guid id)
        {
            return Ok(await _userService.GetUserByIdAsync(id));
        }

        /// <summary>Gets all users in the system.</summary>
        [HttpGet]
        public async Task<ActionResult<List<UserResponseDto>>> GetAllUsers()
        {
            return Ok(await _userService.GetAllUsersAsync());
        }

        /// <summary>
        /// Updates the current user's profile.
        /// Returns 403 if attempting to update another user's profile.
        /// Returns 409 if the new email is already in use.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<UserResponseDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUserDto)
        {
            var currentUserId = GetUserId();

            // ForbiddenException → 403 через GlobalExceptionHandler.
            if (currentUserId != id)
                throw new ForbiddenException("You can only update your own profile.");

            var result = await _userService.UpdateUserAsync(id, updateUserDto);
            _logger.LogInformation("User {UserId} updated", id);
            return Ok(result);
        }

        /// <summary>
        /// Deletes the current user's account and all associated data.
        /// Returns 403 if attempting to delete another user's account.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteUser(Guid id)
        {
            var currentUserId = GetUserId();

            if (currentUserId != id)
                throw new ForbiddenException("You can only delete your own account.");

            await _userService.DeleteUserAsync(id);
            _logger.LogInformation("User {UserId} deleted", id);
            return NoContent();
        }
    }
}