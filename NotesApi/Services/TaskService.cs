using Microsoft.EntityFrameworkCore;
using NotesApi.Data;
using NotesApi.Dtos.Tasks;
using NotesApi.Exceptions;
using NotesApi.Models;

namespace NotesApi.Services
{
    public interface ITaskService
    {
        Task<TaskResponseDto> CreateTaskAsync(Guid userId, CreateTaskDto createTaskDto);
        Task<TaskResponseDto> GetTaskByIdAsync(Guid id, Guid userId);
        Task<List<TaskResponseDto>> GetUserTasksAsync(Guid userId, bool? completed = null);
        Task<List<TaskResponseDto>> GetTasksByPriorityAsync(Guid userId, TaskPriority priority);
        Task<List<TaskResponseDto>> GetOverdueTasksAsync(Guid userId);
        Task<TaskResponseDto> UpdateTaskAsync(Guid id, Guid userId, UpdateTaskDto updateTaskDto);
        Task<bool> DeleteTaskAsync(Guid id, Guid userId);
    }

    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TaskService> _logger;

        public TaskService(AppDbContext context, ILogger<TaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TaskResponseDto> CreateTaskAsync(Guid userId, CreateTaskDto createTaskDto)
        {
            var task = new TaskItem
            {
                Title = createTaskDto.Title.Trim(),
                Description = createTaskDto.Description ?? "",
                DueDate = createTaskDto.DueDate,
                Priority = createTaskDto.Priority,
                UserId = userId,
                IsCompleted = false
            };

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} created for user {UserId}", task.Id, userId);
            return MapToDto(task);
        }

        public async Task<TaskResponseDto> GetTaskByIdAsync(Guid id, Guid userId)
        {
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                throw new NotFoundException("Task", id);

            return MapToDto(task);
        }

        public async Task<List<TaskResponseDto>> GetUserTasksAsync(Guid userId, bool? completed = null)
        {
            var query = _context.TaskItems.Where(t => t.UserId == userId);

            if (completed.HasValue)
                query = query.Where(t => t.IsCompleted == completed.Value);

            var tasks = await query
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            return tasks.Select(MapToDto).ToList();
        }

        public async Task<List<TaskResponseDto>> GetTasksByPriorityAsync(Guid userId, TaskPriority priority)
        {
            var tasks = await _context.TaskItems
                .Where(t => t.UserId == userId && t.Priority == priority && !t.IsCompleted)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            return tasks.Select(MapToDto).ToList();
        }

        public async Task<List<TaskResponseDto>> GetOverdueTasksAsync(Guid userId)
        {
            var tasks = await _context.TaskItems
                .Where(t => t.UserId == userId && !t.IsCompleted && t.DueDate < DateTime.UtcNow)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            return tasks.Select(MapToDto).ToList();
        }

        public async Task<TaskResponseDto> UpdateTaskAsync(Guid id, Guid userId, UpdateTaskDto updateTaskDto)
        {
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                throw new NotFoundException("Task", id);

            if (!string.IsNullOrEmpty(updateTaskDto.Title))
                task.Title = updateTaskDto.Title.Trim();

            if (updateTaskDto.Description != null)
                task.Description = updateTaskDto.Description;

            if (updateTaskDto.IsCompleted.HasValue)
                task.IsCompleted = updateTaskDto.IsCompleted.Value;

            if (updateTaskDto.DueDate.HasValue)
                task.DueDate = updateTaskDto.DueDate.Value;

            if (updateTaskDto.Priority.HasValue)
                task.Priority = updateTaskDto.Priority.Value;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task {TaskId} updated", id);
            return MapToDto(task);
        }

        public async Task<bool> DeleteTaskAsync(Guid id, Guid userId)
        {
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                throw new NotFoundException("Task", id);

            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Task {TaskId} deleted", id);
            return true;
        }

        private static TaskResponseDto MapToDto(TaskItem task) => new()
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            IsCompleted = task.IsCompleted,
            DueDate = task.DueDate,
            Priority = task.Priority,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}