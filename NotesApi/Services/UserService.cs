using Microsoft.EntityFrameworkCore;
using NotesApi.Data;
using NotesApi.Dtos.Users;
using NotesApi.Exceptions;
using NotesApi.Models;

namespace NotesApi.Services
{
    public interface IUserService
    {
        Task<UserResponseDto> GetUserByIdAsync(Guid id);
        Task<List<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(Guid id);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(AppDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserResponseDto> GetUserByIdAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                throw new NotFoundException("User", id);

            return MapToDto(user);
        }

        public async Task<List<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _context.Users.ToListAsync();
            return users.Select(MapToDto).ToList();
        }

        public async Task<UserResponseDto> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    throw new NotFoundException("User", id);

                if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
                {
                    if (await _context.Users.AnyAsync(u => u.Email == updateUserDto.Email))
                        throw new ConflictException("Email is already in use.");

                    user.Email = updateUserDto.Email.Trim();
                }

                if (!string.IsNullOrEmpty(updateUserDto.FirstName))
                    user.FirstName = updateUserDto.FirstName.Trim();

                if (!string.IsNullOrEmpty(updateUserDto.LastName))
                    user.LastName = updateUserDto.LastName.Trim();

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("User {UserId} updated", id);
                return MapToDto(user);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                throw new NotFoundException("User", id);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} deleted", id);
            return true;
        }

        private static UserResponseDto MapToDto(User user) => new()
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