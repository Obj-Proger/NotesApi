using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesApi.Dtos.Notes;
using NotesApi.Services;
using System.Security.Claims;

namespace NotesApi.Controllers
{
    /// <summary>
    /// Manages note CRUD operations for authenticated users.
    /// All endpoints require valid JWT authorization token.
    /// Users can only access and modify their own notes.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ILogger<NotesController> _logger;

        private const string UserIdClaimNotFoundErrorMessage = "Invalid token claims";

        public NotesController(INoteService noteService, ILogger<NotesController> logger)
        {
            _noteService = noteService;
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
        /// Creates a new note for the authenticated user.
        /// </summary>
        /// <param name="createNoteDto">Note content including title, content, and optional color.</param>
        /// <returns>
        /// HTTP 201 (Created) with the newly created note details and its ID in the Location header.
        /// HTTP 400 (Bad Request) if validation fails.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// The returned Location header follows REST conventions pointing to the new resource.
        /// Notes are created in active (non-archived) state by default.
        /// </remarks>
        [HttpPost]
        public async Task<ActionResult<NoteResponseDto>> CreateNote([FromBody] CreateNoteDto createNoteDto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _noteService.CreateNoteAsync(userId, createNoteDto);
                _logger.LogInformation("Note {NoteId} created for user {UserId}", result.Id, userId);
                return CreatedAtAction(nameof(GetNote), new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Create note validation failed");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Retrieves a specific note by ID with ownership verification.
        /// </summary>
        /// <param name="id">The note identifier to retrieve.</param>
        /// <returns>
        /// HTTP 200 (OK) with the note details if the user owns it.
        /// HTTP 404 (Not Found) if note doesn't exist or belongs to another user.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// The endpoint prevents unauthorized access by validating user ownership.
        /// Non-existent notes and notes owned by other users return the same 404 response.
        /// </remarks>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<NoteResponseDto>> GetNote(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _noteService.GetNoteByIdAsync(id, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Note not found");
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting note");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Retrieves all active notes for the authenticated user.
        /// </summary>
        /// <returns>
        /// HTTP 200 (OK) with a list of user's active notes sorted by most recent update first.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Returns only notes with IsArchived set to false.
        /// Empty list is returned if user has no active notes.
        /// </remarks>
        [HttpGet("user/all")]
        public async Task<ActionResult<List<NoteResponseDto>>> GetUserNotes()
        {
            try
            {
                var userId = GetUserId();
                var result = await _noteService.GetUserNotesAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user notes");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Retrieves all archived notes for the authenticated user.
        /// </summary>
        /// <returns>
        /// HTTP 200 (OK) with a list of user's archived notes sorted by most recent update first.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Returns only notes with IsArchived set to true.
        /// Empty list is returned if user has no archived notes.
        /// </remarks>
        [HttpGet("user/archived")]
        public async Task<ActionResult<List<NoteResponseDto>>> GetArchivedNotes()
        {
            try
            {
                var userId = GetUserId();
                var result = await _noteService.GetArchivedNotesAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting archived notes");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Updates an existing note with new content or metadata.
        /// Supports partial updates - only provided fields are modified.
        /// </summary>
        /// <param name="id">The note identifier to update.</param>
        /// <param name="updateNoteDto">Updated note fields. Null/empty fields are ignored.</param>
        /// <returns>
        /// HTTP 200 (OK) with the updated note details.
        /// HTTP 400 (Bad Request) if validation fails.
        /// HTTP 404 (Not Found) if note doesn't exist or belongs to another user.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Allows toggling note archive status independently from content updates.
        /// UpdatedAt timestamp is automatically set to current UTC time.
        /// </remarks>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<NoteResponseDto>> UpdateNote(Guid id, [FromBody] UpdateNoteDto updateNoteDto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _noteService.UpdateNoteAsync(id, userId, updateNoteDto);
                _logger.LogInformation("Note {NoteId} updated by user {UserId}", id, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Note not found");
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Update note validation failed");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Permanently deletes a note with ownership verification.
        /// This operation is irreversible.
        /// </summary>
        /// <param name="id">The note identifier to delete.</param>
        /// <returns>
        /// HTTP 204 (No Content) on successful deletion.
        /// HTTP 404 (Not Found) if note doesn't exist or belongs to another user.
        /// HTTP 500 (Internal Server Error) for unexpected failures.
        /// </returns>
        /// <remarks>
        /// Deleted notes cannot be recovered. Consider implementing soft delete or archive feature.
        /// </remarks>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteNote(Guid id)
        {
            try
            {
                var userId = GetUserId();
                await _noteService.DeleteNoteAsync(id, userId);
                _logger.LogInformation("Note {NoteId} deleted by user {UserId}", id, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Note not found");
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}