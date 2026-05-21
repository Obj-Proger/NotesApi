using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesApi.Dtos.Users;
using NotesApi.Services;
using System.Security.Claims;

namespace NotesApi.Controllers
{
    /// <summary>
    /// Manages user profile operations for authenticated users.
    /// Users can only view all profiles but modify only their own.
    /// All endpoints require valid JWT authorization token.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        private const string UserIdClaimNotFoundErrorMessage = "Invalid token claims";

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Extracts the authenticated user's ID from JWT claims.
        /// </summary>
        /// <returns>The user ID parsed from NameIdentifier claim.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when NameIdentifier claim is missing or invalid format.</exception>
        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(claim, out var userId))
                return userId;
            throw new UnauthorizedAccessException(UserIdClaimNotFoundErrorMessage);
        }

        /// <summary>
        /// Retrieves a user's public profile information by ID.
        /// </summary>
        /// <param name="id">The user identifier to retrieve.</param>
        /// <returns>
        /// HTTP 200 (OK) with user profile information.
        /// HTTP 404 (Not Found) if user doesn't exist.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Returns only public profile data (username, email, names).
        /// Sensitive data such as password hash is never returned.
        /// Any authenticated user can view any user's profile.
        /// </remarks>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(Guid id)
        {
            try
            {
                var result = await _userService.GetUserByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found");
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Retrieves all users in the system with public profile information.
        /// Warning: This endpoint should be restricted to admin users in production.
        /// </summary>
        /// <returns>
        /// HTTP 200 (OK) with a list of all user profiles.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Returns public profile data only, excluding sensitive information.
        /// Consider implementing pagination for scalability with large user bases.
        /// Consider adding admin-only access restriction in production.
        /// </remarks>
        [HttpGet]
        public async Task<ActionResult<List<UserResponseDto>>> GetAllUsers()
        {
            try
            {
                var result = await _userService.GetAllUsersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Updates the authenticated user's profile information.
        /// Users can only modify their own profiles, not others'.
        /// Supports partial updates - only provided fields are modified.
        /// </summary>
        /// <param name="id">The user identifier to update.</param>
        /// <param name="updateUserDto">Updated user profile fields. Null/empty fields are ignored.</param>
        /// <returns>
        /// HTTP 200 (OK) with the updated user profile.
        /// HTTP 400 (Bad Request) if validation fails (e.g., email already in use).
        /// HTTP 403 (Forbidden) when attempting to modify another user's profile.
        /// HTTP 404 (Not Found) if user doesn't exist.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Email is validated for uniqueness before update. Username cannot be changed.
        /// UpdatedAt timestamp is automatically set to current UTC time.
        /// Fails safely if user attempts to modify another user's account.
        /// </remarks>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<UserResponseDto>> UpdateUser(Guid id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var currentUserId = GetUserId();

                // Prevent users from modifying other users' profiles.
                if (currentUserId != id)
                    return Forbid();

                var result = await _userService.UpdateUserAsync(id, updateUserDto);
                _logger.LogInformation("User {UserId} updated", id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found");
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Update validation failed");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Permanently deletes a user account and all associated data.
        /// Users can only delete their own accounts, not others'.
        /// This operation is irreversible and removes all related notes and tasks.
        /// </summary>
        /// <param name="id">The user identifier to delete.</param>
        /// <returns>
        /// HTTP 204 (No Content) on successful deletion.
        /// HTTP 403 (Forbidden) when attempting to delete another user's account.
        /// HTTP 404 (Not Found) if user doesn't exist.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Deletion cascade removes all associated notes and tasks from the database.
        /// Deleted accounts cannot be recovered. Consider implementing soft delete.
        /// All active tokens for the deleted user remain valid until expiration.
        /// </remarks>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteUser(Guid id)
        {
            try
            {
                var currentUserId = GetUserId();

                // Prevent users from deleting other users' accounts.
                if (currentUserId != id)
                    return Forbid();

                await _userService.DeleteUserAsync(id);
                _logger.LogInformation("User {UserId} deleted", id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found");
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}