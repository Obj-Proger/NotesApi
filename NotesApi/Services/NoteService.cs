using Microsoft.EntityFrameworkCore;
using NotesApi.Data;
using NotesApi.Dtos.Notes;
using NotesApi.Exceptions;
using NotesApi.Models;

namespace NotesApi.Services
{
    public interface INoteService
    {
        Task<NoteResponseDto> CreateNoteAsync(Guid userId, CreateNoteDto createNoteDto);
        Task<NoteResponseDto> GetNoteByIdAsync(Guid id, Guid userId);
        Task<List<NoteResponseDto>> GetUserNotesAsync(Guid userId);
        Task<List<NoteResponseDto>> GetArchivedNotesAsync(Guid userId);
        Task<NoteResponseDto> UpdateNoteAsync(Guid id, Guid userId, UpdateNoteDto updateNoteDto);
        Task<bool> DeleteNoteAsync(Guid id, Guid userId);
    }

    public class NoteService : INoteService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NoteService> _logger;

        private const string DefaultNoteColor = "#FFFFFF";

        public NoteService(AppDbContext context, ILogger<NoteService> logger)
        {
            _context = context;
            _logger = logger;
        }

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

        public async Task<NoteResponseDto> GetNoteByIdAsync(Guid id, Guid userId)
        {
            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
                throw new NotFoundException("Note", id);

            return MapToDto(note);
        }

        public async Task<List<NoteResponseDto>> GetUserNotesAsync(Guid userId)
        {
            var notes = await _context.Notes
                .Where(n => n.UserId == userId && !n.IsArchived)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            return notes.Select(MapToDto).ToList();
        }

        public async Task<List<NoteResponseDto>> GetArchivedNotesAsync(Guid userId)
        {
            var notes = await _context.Notes
                .Where(n => n.UserId == userId && n.IsArchived)
                .OrderByDescending(n => n.UpdatedAt)
                .ToListAsync();

            return notes.Select(MapToDto).ToList();
        }

        public async Task<NoteResponseDto> UpdateNoteAsync(Guid id, Guid userId, UpdateNoteDto updateNoteDto)
        {
            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
                throw new NotFoundException("Note", id);

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

        public async Task<bool> DeleteNoteAsync(Guid id, Guid userId)
        {
            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
                throw new NotFoundException("Note", id);

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Note {NoteId} deleted", id);
            return true;
        }

        private static NoteResponseDto MapToDto(Note note) => new()
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