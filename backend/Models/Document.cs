namespace backend.Models
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid CreatedById { get; set; }
        public User CreatedBy { get; set; } = null!; 

        public ICollection<DocumentSection> Sections { get; set; } = new List<DocumentSection>();
    }
}