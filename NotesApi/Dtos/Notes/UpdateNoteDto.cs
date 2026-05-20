namespace NotesApi.Dtos.Notes
{
    public class UpdateNoteDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Color { get; set; }
        public bool? IsArchived { get; set; }
    }
}