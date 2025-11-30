using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class Image
    {

        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string FileName { get; set; }
        [Required]
        public string Path { get; set; }
        public Guid? UploadedById { get; set; }
        public User UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public bool OcrProcessed { get; set; } = false;
        public OcrJob OcrJob { get; set; }
        public List<TextFile> TextFiles { get; set; } = new();
    }
}