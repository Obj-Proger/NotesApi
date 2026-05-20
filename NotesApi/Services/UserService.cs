using Microsoft.EntityFrameworkCore;
using NotesApi.Data;
using NotesApi.Dtos.Users;
using NotesApi.Models;

namespace NotesApi.Services
{
    /// <summary>
    /// Defines user profile management operations including retrieval and modification.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Retrieves a user's profile by identifier.
        /// </summary>
        /// <param name="id">The user identifier.</param>
        /// <returns>The user profile information.</returns>
        Task<UserResponseDto> GetUserByIdAsync(Guid id);

        /// <summary>
        /// Retrieves all users in the system.
        /// Note: Consider implementing pagination and access control for production.
        /// </summary>
        /// <returns>List of all user profiles.</returns>
        Task<List<UserResponseDto>> GetAllUsersAsync();

        /// <summary>
        /// Updates a user's profile information with selective modifications.
        /// </summary>
        /// <param name="id">The user identifier to update.</param>
        /// <param name="updateUserDto">Updated user data.</param>
        /// <returns>The updated user profile.</returns>
        Task<UserResponseDto> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);

        /// <summary>
        /// Permanently deletes a user account and all associated data.
        /// This operation is irreversible.
        /// </summary>
        /// <param name="id">The user identifier to delete.</param>
        /// <returns>True if deletion was successful.</returns>
        Task<bool> DeleteUserAsync(Guid id);
    }

    /// <summary>
    /// Implements user profile management with email uniqueness validation.
    /// Cascading deletes remove all associated notes and tasks when a user is deleted.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;

        private const string UserNotFoundMessageTemplate = "User with id {0} not found";
        private const string EmailAlreadyInUseErrorMessage = "Email already in use";

        public UserService(AppDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a user's profile by ID.
        /// Returns public user information without sensitive data.
        /// </summary>
        public async Task<UserResponseDto> GetUserByIdAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                throw new KeyNotFoundException(string.Format(UserNotFoundMessageTemplate, id));

            return MapToDto(user);
        }

        /// <summary>
        /// Retrieves all users in the system.
        /// Warning: This endpoint should be restricted to admin users in production.
        /// </summary>
        public async Task<List<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _context.Users.ToListAsync();
            return users.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Updates user profile with selective field modifications.
        /// Validates email uniqueness to prevent duplicate accounts.
        /// Uses transaction to ensure data consistency.
        /// </summary>
        public async Task<UserResponseDto> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    throw new KeyNotFoundException(string.Format(UserNotFoundMessageTemplate, id));

                // Validate email uniqueness before applying change.
                if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
                {
                    if (await _context.Users.AnyAsync(u => u.Email == updateUserDto.Email))
                        throw new InvalidOperationException(EmailAlreadyInUseErrorMessage);

                    user.Email = updateUserDto.Email.Trim();
                }

                // Apply optional profile updates with normalization.
                if (!string.IsNullOrEmpty(updateUserDto.FirstName))
                    user.FirstName = updateUserDto.FirstName.Trim();

                if (!string.IsNullOrEmpty(updateUserDto.LastName))
                    user.LastName = updateUserDto.LastName.Trim();

                // UpdatedAt is automatically set by SaveChangesAsync override.
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("User {UserId} updated", id);
                return MapToDto(user);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating user");
                throw;
            }
        }

        /// <summary>
        /// Permanently deletes a user account and cascades deletion to related data.
        /// Associated notes and tasks are automatically deleted by database constraints.
        /// This operation is irreversible.
        /// </summary>
        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                throw new KeyNotFoundException(string.Format(UserNotFoundMessageTemplate, id));

            // Cascading delete removes associated notes and tasks via configured relationships.
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} deleted", id);
            return true;
        }

        /// <summary>
        /// Maps a User entity to its DTO representation for API responses.
        /// Excludes sensitive data such as password hash and refresh tokens.
        /// </summary>
        private static UserResponseDto MapToDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}