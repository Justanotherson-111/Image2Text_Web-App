namespace backend.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Token { get; set; } = null!;

        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; } = false;

        // Foreign key
        public Guid UserId { get; set; }

        // Navigation
        public User User { get; set; } = null!;
    }
}
