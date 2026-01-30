namespace backend.Models
{
    public class DocumentSection
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = null!;

        public string? Title { get; set; }
        public int OrderIndex { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Image> Images { get; set; } = new List<Image>();
    }
}