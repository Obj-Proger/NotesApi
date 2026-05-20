using Microsoft.EntityFrameworkCore;
using NotesApi.Data;
using NotesApi.Dtos.Notes;
using NotesApi.Models;

namespace NotesApi.Services
{
    /// <summary>
    /// Defines note management operations for authenticated users.
    /// </summary>
    public interface INoteService
    {
        /// <summary>
        /// Creates a new note for the specified user.
        /// </summary>
        /// <param name="userId">The owner of the note.</param>
        /// <param name="createNoteDto">Note creation data.</param>
        /// <returns>The created note as DTO.</returns>
        Task<NoteResponseDto> CreateNoteAsync(Guid userId, CreateNoteDto createNoteDto);

        /// <summary>
        /// Retrieves a note by ID with ownership verification.
        /// </summary>
        /// <param name="id">The note identifier.</param>
        /// <param name="userId">The requesting user's identifier.</param>
        /// <returns>The note details if owned by the user.</returns>
        Task<NoteResponseDto> GetNoteByIdAsync(Guid id, Guid userId);

        /// <summary>
        /// Retrieves all active (non-archived) notes for a user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>List of active notes sorted by most recent update first.</returns>
        Task<List<NoteResponseDto>> GetUserNotesAsync(Guid userId);

        /// <summary>
        /// Retrieves all archived notes for a user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>List of archived notes sorted by most recent update first.</returns>
        Task<List<NoteResponseDto>> GetArchivedNotesAsync(Guid userId);

        /// <summary>
        /// Updates an existing note with new content or metadata.
        /// </summary>
        /// <param name="id">The note identifier to update.</param>
        /// <param name="userId">The requesting user's identifier.</param>
        /// <param name="updateNoteDto">Updated note data.</param>
        /// <returns>The updated note details.</returns>
        Task<NoteResponseDto> UpdateNoteAsync(Guid id, Guid userId, UpdateNoteDto updateNoteDto);

        /// <summary>
        /// Permanently deletes a note.
        /// </summary>
        /// <param name="id">The note identifier to delete.</param>
        /// <param name="userId">The requesting user's identifier.</param>
        /// <returns>True if deletion was successful.</returns>
        Task<bool> DeleteNoteAsync(Guid id, Guid userId);
    }

    /// <summary>
    /// Implements CRUD operations for user notes with archiving support.
    /// Ensures data isolation by validating user ownership on all operations.
    /// </summary>
    public class NoteService : INoteService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NoteService> _logger;

        private const string DefaultNoteColor = "#FFFFFF";
        private const string NoteNotFoundMessageTemplate = "Note with id {0} not found";

        public NoteService(AppDbContext context, ILogger<NoteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new note with normalized data and sets initial state.
        /// </summary>
        public async Task<NoteResponseDto> CreateNoteAsync(Guid userId, CreateNoteDto createNoteDto)
        {
            var note = new Note
            {
                Title = createNoteDto.Title.Trim(),
                Content = createNoteDto.Content ?? "",
                Color = createNoteDto.Color ?? DefaultNoteColor,
                UserId = userId,
                IsArchived = false
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Note {NoteId} created for user {UserId}", note.Id, userId);
            return MapToDto(note);
        }

        /// <summary>
        /// Retrieves a note by ID with ownership verification.
        /// Prevents unauthorized access to notes belonging to other users.
        /// </summary>
        public async Task<NoteResponseDto> GetNoteByIdAsync(Guid id, Guid userId)
        {
            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
                throw new KeyNotFoundException(string.Format(NoteNotFoundMessageTemplate, id));

            return MapToDto(note);
        }

        /// <summary>
        /// Retrieves all active notes for a user sorted by modification date.
        /// Active notes are those with IsArchived set to false.
        /// </summary>
        public async Task<List<NoteResponseDto>> GetUserNotesAsync(Guid userId)
        {
            var notes = await _context.Notes
                .Where(n => n.UserId == userId && !n.IsArchived)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            return notes.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Retrieves all archived notes for a user sorted by modification date.
        /// Archived notes are those with IsArchived set to true.
        /// </summary>
        public async Task<List<NoteResponseDto>> GetArchivedNotesAsync(Guid userId)
        {
            var notes = await _context.Notes
                .Where(n => n.UserId == userId && n.IsArchived)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            return notes.Select(MapToDto).ToList();
        }

        /// <summary>
        /// Updates a note's content and metadata with selective updates.
        /// Only non-null/non-empty properties from the DTO are applied.
        /// UpdatedAt timestamp is automatically set by SaveChangesAsync.
        /// </summary>
        public async Task<NoteResponseDto> UpdateNoteAsync(Guid id, Guid userId, UpdateNoteDto updateNoteDto)
        {
            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
                throw new KeyNotFoundException(string.Format(NoteNotFoundMessageTemplate, id));

            // Apply only provided updates to allow partial modifications.
            if (!string.IsNullOrEmpty(updateNoteDto.Title))
                note.Title = updateNoteDto.Title.Trim();

            if (updateNoteDto.Content != null)
                note.Content = updateNoteDto.Content;

            if (updateNoteDto.Color != null)
                note.Color = updateNoteDto.Color;

            if (updateNoteDto.IsArchived.HasValue)
                note.IsArchived = updateNoteDto.IsArchived.Value;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Note {NoteId} updated", id);
            return MapToDto(note);
        }

        /// <summary>
        /// Permanently deletes a note with ownership verification.
        /// Cascading delete rules in database remove associated data.
        /// </summary>
        public async Task<bool> DeleteNoteAsync(Guid id, Guid userId)
        {
            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
                throw new KeyNotFoundException(string.Format(NoteNotFoundMessageTemplate, id));

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Note {NoteId} deleted", id);
            return true;
        }

        /// <summary>
        /// Maps a Note entity to its DTO representation for API responses.
        /// </summary>
        private static NoteResponseDto MapToDto(Note note)
        {
            return new NoteResponseDto
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Content,
                Color = note.Color,
                IsArchived = note.IsArchived,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            };
        }
    }
}