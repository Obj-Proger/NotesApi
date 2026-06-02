using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotesApi.Dtos.Notes;
using NotesApi.Services;

namespace NotesApi.Controllers
{
    /// <summary>
    /// Manages note CRUD operations for authenticated users.
    /// All exceptions bubble up to GlobalExceptionHandler.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class NotesController : ApiControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ILogger<NotesController> _logger;

        public NotesController(INoteService noteService, ILogger<NotesController> logger)
        {
            _noteService = noteService;
            _logger = logger;
        }

        /// <summary>Creates a new note. Returns 201 with note details.</summary>
        [HttpPost]
        public async Task<ActionResult<NoteResponseDto>> CreateNote([FromBody] CreateNoteDto createNoteDto)
        {
            var userId = GetUserId();
            var result = await _noteService.CreateNoteAsync(userId, createNoteDto);
            _logger.LogInformation("Note {NoteId} created for user {UserId}", result.Id, userId);
            return CreatedAtAction(nameof(GetNote), new { id = result.Id }, result);
        }

        /// <summary>Gets a note by ID. Returns 404 if not found or owned by another user.</summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<NoteResponseDto>> GetNote(Guid id)
        {
            var userId = GetUserId();
            var result = await _noteService.GetNoteByIdAsync(id, userId);
            return Ok(result);
        }

        /// <summary>Gets all active (non-archived) notes for the current user.</summary>
        [HttpGet("user/all")]
        public async Task<ActionResult<List<NoteResponseDto>>> GetUserNotes()
        {
            var userId = GetUserId();
            return Ok(await _noteService.GetUserNotesAsync(userId));
        }

        /// <summary>Gets all archived notes for the current user.</summary>
        [HttpGet("user/archived")]
        public async Task<ActionResult<List<NoteResponseDto>>> GetArchivedNotes()
        {
            var userId = GetUserId();
            return Ok(await _noteService.GetArchivedNotesAsync(userId));
        }

        /// <summary>Updates a note. Returns 404 if not found.</summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<NoteResponseDto>> UpdateNote(Guid id, [FromBody] UpdateNoteDto updateNoteDto)
        {
            var userId = GetUserId();
            var result = await _noteService.UpdateNoteAsync(id, userId, updateNoteDto);
            _logger.LogInformation("Note {NoteId} updated by user {UserId}", id, userId);
            return Ok(result);
        }

        /// <summary>Permanently deletes a note. Returns 204 on success, 404 if not found.</summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteNote(Guid id)
        {
            var userId = GetUserId();
            await _noteService.DeleteNoteAsync(id, userId);
            _logger.LogInformation("Note {NoteId} deleted by user {UserId}", id, userId);
            return NoContent();
        }
    }
}