namespace backend.DTOs
{
    public record RegisterDto(string Username, string Email, string Password);
    public record LoginDto(string Username, string Password);
    public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
    public record UploadImageDto(Guid? UploadedBy = null);
    public record TextFileDto(Guid Id, string FileName, string Path, Guid ImageId, DateTime CreatedAt);
    public class RefreshRequestDto { public string RefreshToken { get; set; } }
}