using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesApi.Dtos.Tasks;
using NotesApi.Models;
using NotesApi.Services;

namespace NotesApi.Controllers
{
    /// <summary>
    /// Manages task CRUD and filtering for authenticated users.
    /// All exceptions bubble up to GlobalExceptionHandler.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ApiControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        /// <summary>Creates a new task. Returns 201 with task details.</summary>
        [HttpPost]
        public async Task<ActionResult<TaskResponseDto>> CreateTask([FromBody] CreateTaskDto createTaskDto)
        {
            var userId = GetUserId();
            var result = await _taskService.CreateTaskAsync(userId, createTaskDto);
            _logger.LogInformation("Task created for user {UserId}", userId);
            return CreatedAtAction(nameof(GetTask), new { id = result.Id }, result);
        }

        /// <summary>Gets a task by ID. Returns 404 if not found or owned by another user.</summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TaskResponseDto>> GetTask(Guid id)
        {
            var userId = GetUserId();
            return Ok(await _taskService.GetTaskByIdAsync(id, userId));
        }

        /// <summary>Gets all tasks for the current user, optionally filtered by completion status.</summary>
        [HttpGet("user/all")]
        public async Task<ActionResult<List<TaskResponseDto>>> GetUserTasks([FromQuery] bool? completed = null)
        {
            var userId = GetUserId();
            return Ok(await _taskService.GetUserTasksAsync(userId, completed));
        }

        /// <summary>Gets incomplete tasks filtered by priority level.</summary>
        [HttpGet("user/priority/{priority}")]
        public async Task<ActionResult<List<TaskResponseDto>>> GetTasksByPriority(TaskPriority priority)
        {
            var userId = GetUserId();
            return Ok(await _taskService.GetTasksByPriorityAsync(userId, priority));
        }

        /// <summary>Gets incomplete tasks past their due date.</summary>
        [HttpGet("user/overdue")]
        public async Task<ActionResult<List<TaskResponseDto>>> GetOverdueTasks()
        {
            var userId = GetUserId();
            return Ok(await _taskService.GetOverdueTasksAsync(userId));
        }

        /// <summary>Updates a task. Returns 404 if not found.</summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<TaskResponseDto>> UpdateTask(Guid id, [FromBody] UpdateTaskDto updateTaskDto)
        {
            var userId = GetUserId();
            var result = await _taskService.UpdateTaskAsync(id, userId, updateTaskDto);
            _logger.LogInformation("Task {TaskId} updated", id);
            return Ok(result);
        }

        /// <summary>Permanently deletes a task. Returns 204 on success, 404 if not found.</summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteTask(Guid id)
        {
            var userId = GetUserId();
            await _taskService.DeleteTaskAsync(id, userId);
            _logger.LogInformation("Task {TaskId} deleted", id);
            return NoContent();
        }
    }
}