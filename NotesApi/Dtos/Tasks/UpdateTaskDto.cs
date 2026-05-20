using NotesApi.Models;

namespace NotesApi.Dtos.Tasks
{
    public class UpdateTaskDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool? IsCompleted { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskPriority? Priority { get; set; }
    }
}