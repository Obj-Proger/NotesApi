namespace NotesApi.Dtos.Notes
{
    public class CreateNoteDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Color { get; set; }
    }
}