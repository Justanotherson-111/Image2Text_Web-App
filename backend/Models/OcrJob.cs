namespace backend.Models
{
    public class OcrJob
    {
        public Guid Id { get; set; } = Guid.NewGuid(); 
        public Guid ImageId { get; set; }
        public Image Image { get; set; }
        public bool Completed { get; set; } = false; 
        public string? ResultPath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
        public DateTime? CompletedAt { get; set; }
    }
}