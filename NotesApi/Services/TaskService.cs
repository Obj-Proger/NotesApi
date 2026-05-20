using Microsoft.EntityFrameworkCore;
using NotesApi.Data;
using NotesApi.Dtos.Tasks;
using NotesApi.Models;

namespace NotesApi.Services
{
    /// <summary>
    /// Defines task management operations including creation, retrieval, filtering, and deletion.
    /// </summary>
    public interface ITaskService
    {
        /// <summary>
        /// Creates a new task for the specified user.
        /// </summary>
        /// <param name="userId">The task owner's identifier.</param>
        /// <param name="createTaskDto">Task creation data.</param>
        /// <returns>The created task as DTO.</returns>
        Task<TaskResponseDto> CreateTaskAsync(Guid userId, CreateTaskDto createTaskDto);

        /// <summary>
        /// Retrieves a specific task by ID with ownership verification.
        /// </summary>
        /// <param name="id">The task identifier.</param>
        /// <param name="userId">The requesting user's identifier.</param>
        /// <returns>The task details if owned by the user.</returns>
        Task<TaskResponseDto> GetTaskByIdAsync(Guid id, Guid userId);

        /// <summary>
        /// Retrieves all tasks for a user with optional completion status filter.
        /// Results are sorted by priority descending, then by due date ascending.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="completed">Filter by completion status (true/false) or null for all tasks.</param>
        /// <returns>Filtered and sorted list of tasks.</returns>
        Task<List<TaskResponseDto>> GetUserTasksAsync(Guid userId, bool? completed = null);

        /// <summary>
        /// Retrieves incomplete tasks for a user filtered by priority level.
        /// Results are sorted by due date in ascending order.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="priority">The priority level to filter by.</param>
        /// <returns>List of incomplete tasks with matching priority.</returns>
        Task<List<TaskResponseDto>> GetTasksByPriorityAsync(Guid userId, TaskPriority priority);

        /// <summary>
        /// Retrieves incomplete tasks with due dates in the past.
        /// Results are sorted by due date in ascending order.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>List of overdue incomplete tasks.</returns>
        Task<List<TaskResponseDto>> GetOverdueTasksAsync(Guid userId);

        /// <summary>
        /// Updates an existing task with new values or metadata.
        /// </summary>
        /// <param name="id">The task identifier to update.</param>
        /// <param name="userId">The requesting user's identifier.</param>
        /// <param name="updateTaskDto">Updated task data.</param>
        /// <returns>The updated task details.</returns>
        Task<TaskResponseDto> UpdateTaskAsync(Guid id, Guid userId, UpdateTaskDto updateTaskDto);

        /// <summary>
        /// Permanently deletes a task.
        /// </summary>
        /// <param name="id">The task identifier to delete.</param>
        /// <param name="userId">The requesting user's identifier.</param>
        /// <returns>True if deletion was successful.</returns>
        Task<bool> DeleteTaskAsync(Guid id, Guid userId);
    }

    /// <summary>
    /// Implements task management operations with priority-based sorting and due date tracking.
    /// Ensures data isolation by verifying user ownership on all operations.
    /// </summary>
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TaskService> _logger;

        private const string TaskNotFoundMessageTemplate = "Task with id {0} not found";

        public TaskService(AppDbContext context, ILogger<TaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new task with normalized data and sets incomplete state.
        /// </summary>
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

        /// <summary>
        /// Retrieves a task by ID with ownership verification.
        /// Prevents unauthorized access to tasks belonging to other users.
        /// </summary>
        public async Task<TaskResponseDto> GetTaskByIdAsync(Guid id, Guid userId)
        {
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                throw new KeyNotFoundException(string.Format(TaskNotFoundMessageTemplate, id));

            return MapToDto(task);
        }

        /// <summary>
        /// Retrieves all tasks for a user with optional completion filtering.
        /// Sorted by priority descending (high priority first), then by due date ascending.
        /// </summary>
        public async Task<List<TaskResponseDto>> GetUserTasksAsync(Guid userId, bool? completed = null)
        {
            var query = _context.TaskItems.Where(t => t.UserId == userId);

            // Apply completion status filter if provided.
            if (completed.HasValue)
                query = query.Where(t => t.IsCompleted == completed.Value);

            var tasks = await query
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            return tasks.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Retrieves only incomplete tasks for a user filtered by priority level.
        /// Results are sorted by due date to highlight urgent deadlines.
        /// </summary>
        public async Task<List<TaskResponseDto>> GetTasksByPriorityAsync(Guid userId, TaskPriority priority)
        {
            var tasks = await _context.TaskItems
                .Where(t => t.UserId == userId && t.Priority == priority && !t.IsCompleted)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            return tasks.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Retrieves incomplete tasks with due dates in the past (overdue).
        /// Sorted by due date to show most overdue tasks first.
        /// </summary>
        public async Task<List<TaskResponseDto>> GetOverdueTasksAsync(Guid userId)
        {
            var tasks = await _context.TaskItems
                .Where(t => t.UserId == userId && !t.IsCompleted && t.DueDate < DateTime.UtcNow)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            return tasks.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Updates a task's properties with selective updates.
        /// Only non-null/non-empty properties from the DTO are applied.
        /// UpdatedAt timestamp is automatically set by SaveChangesAsync.
        /// </summary>
        public async Task<TaskResponseDto> UpdateTaskAsync(Guid id, Guid userId, UpdateTaskDto updateTaskDto)
        {
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                throw new KeyNotFoundException(string.Format(TaskNotFoundMessageTemplate, id));

            // Apply only provided updates to allow partial modifications.
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

        /// <summary>
        /// Permanently deletes a task with ownership verification.
        /// Cascading delete rules in database remove associated data.
        /// </summary>
        public async Task<bool> DeleteTaskAsync(Guid id, Guid userId)
        {
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                throw new KeyNotFoundException(string.Format(TaskNotFoundMessageTemplate, id));

            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Task {TaskId} deleted", id);
            return true;
        }

        /// <summary>
        /// Maps a TaskItem entity to its DTO representation for API responses.
        /// </summary>
        private static TaskResponseDto MapToDto(TaskItem task)
        {
            return new TaskResponseDto
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
}