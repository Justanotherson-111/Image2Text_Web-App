using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public enum Role { User, Admin }
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required] 
        public string Username { get; set; }
        [Required] 
        public string PasswordHash { get; set; }
        [Required] 
        public string Email { get; set; }
        public Role UserRole { get; set; } = Role.User;
        public List<RefreshToken> RefreshTokens { get; set; } = new();
        public List<Image> Images { get; set; } = new();
        public List<TextFile> TextFiles { get; set; } = new();
    }
}