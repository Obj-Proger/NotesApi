using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesApi.Dtos.Tasks;
using NotesApi.Models;
using NotesApi.Services;
using System.Security.Claims;

namespace NotesApi.Controllers
{
    /// <summary>
    /// Manages task CRUD operations and filtering for authenticated users.
    /// Supports filtering by completion status, priority level, and due date.
    /// All endpoints require valid JWT authorization token.
    /// Users can only access and modify their own tasks.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        private const string UserIdClaimNotFoundErrorMessage = "Invalid token claims";

        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
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
        /// Creates a new task for the authenticated user.
        /// </summary>
        /// <param name="createTaskDto">Task data including title, description, priority, and due date.</param>
        /// <returns>
        /// HTTP 201 (Created) with the newly created task and its ID in the Location header.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Tasks are created in incomplete state by default.
        /// The Location header follows REST conventions pointing to the new resource.
        /// </remarks>
        [HttpPost]
        public async Task<ActionResult<TaskResponseDto>> CreateTask([FromBody] CreateTaskDto createTaskDto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _taskService.CreateTaskAsync(userId, createTaskDto);
                _logger.LogInformation("Task created for user {UserId}", userId);
                return CreatedAtAction(nameof(GetTask), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Retrieves a specific task by ID with ownership verification.
        /// </summary>
        /// <param name="id">The task identifier to retrieve.</param>
        /// <returns>
        /// HTTP 200 (OK) with the task details if the user owns it.
        /// HTTP 404 (Not Found) if task doesn't exist or belongs to another user.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// The endpoint prevents unauthorized access by validating user ownership.
        /// Non-existent tasks and tasks owned by other users return the same 404 response.
        /// </remarks>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TaskResponseDto>> GetTask(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _taskService.GetTaskByIdAsync(id, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Task not found");
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Retrieves all tasks for the authenticated user with optional completion filtering.
        /// </summary>
        /// <param name="completed">Optional filter: true for completed tasks, false for incomplete, null for all.</param>
        /// <returns>
        /// HTTP 200 (OK) with a list of tasks sorted by priority (descending) then due date (ascending).
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Empty list is returned if no tasks match the filter criteria.
        /// High priority tasks appear first, followed by medium and low priority tasks.
        /// </remarks>
        [HttpGet("user/all")]
        public async Task<ActionResult<List<TaskResponseDto>>> GetUserTasks([FromQuery] bool? completed = null)
        {
            try
            {
                var userId = GetUserId();
                var result = await _taskService.GetUserTasksAsync(userId, completed);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user tasks");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Retrieves incomplete tasks for the authenticated user filtered by priority level.
        /// </summary>
        /// <param name="priority">The priority level to filter by (Low, Medium, High, Critical).</param>
        /// <returns>
        /// HTTP 200 (OK) with a list of incomplete tasks at the specified priority sorted by due date.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Only returns incomplete (IsCompleted = false) tasks.
        /// Results are sorted by due date to show urgent deadlines first.
        /// Empty list is returned if no tasks match the specified priority.
        /// </remarks>
        [HttpGet("user/priority/{priority}")]
        public async Task<ActionResult<List<TaskResponseDto>>> GetTasksByPriority(TaskPriority priority)
        {
            try
            {
                var userId = GetUserId();
                var result = await _taskService.GetTasksByPriorityAsync(userId, priority);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks by priority");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Retrieves incomplete tasks that are past their due date.
        /// </summary>
        /// <returns>
        /// HTTP 200 (OK) with a list of overdue tasks sorted by due date (oldest first).
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Only returns incomplete (IsCompleted = false) tasks with DueDate less than current UTC time.
        /// Results are sorted by due date to show most overdue tasks first.
        /// Empty list is returned if user has no overdue tasks.
        /// </remarks>
        [HttpGet("user/overdue")]
        public async Task<ActionResult<List<TaskResponseDto>>> GetOverdueTasks()
        {
            try
            {
                var userId = GetUserId();
                var result = await _taskService.GetOverdueTasksAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overdue tasks");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Updates an existing task with new values or metadata.
        /// Supports partial updates - only provided fields are modified.
        /// </summary>
        /// <param name="id">The task identifier to update.</param>
        /// <param name="updateTaskDto">Updated task fields. Null/empty fields are ignored.</param>
        /// <returns>
        /// HTTP 200 (OK) with the updated task details.
        /// HTTP 404 (Not Found) if task doesn't exist or belongs to another user.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Allows toggling completion status independently from other field updates.
        /// UpdatedAt timestamp is automatically set to current UTC time.
        /// </remarks>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<TaskResponseDto>> UpdateTask(Guid id, [FromBody] UpdateTaskDto updateTaskDto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _taskService.UpdateTaskAsync(id, userId, updateTaskDto);
                _logger.LogInformation("Task {TaskId} updated", id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Task not found");
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Permanently deletes a task with ownership verification.
        /// This operation is irreversible.
        /// </summary>
        /// <param name="id">The task identifier to delete.</param>
        /// <returns>
        /// HTTP 204 (No Content) on successful deletion.
        /// HTTP 404 (Not Found) if task doesn't exist or belongs to another user.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Deleted tasks cannot be recovered. Consider implementing soft delete or archive feature.
        /// </remarks>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteTask(Guid id)
        {
            try
            {
                var userId = GetUserId();
                await _taskService.DeleteTaskAsync(id, userId);
                _logger.LogInformation("Task {TaskId} deleted", id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Task not found");
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}