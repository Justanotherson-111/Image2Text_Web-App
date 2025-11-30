using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class TextFile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string FileName { get; set; }
        [Required]
        public string Path { get; set; }
        public Guid ImageId { get; set; }
        public Image Image { get; set; }
        public Guid? CreatedById { get; set; }
        public User CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}