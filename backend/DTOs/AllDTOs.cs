namespace backend.DTOs
{
    public record RegisterDto(string Username, string Email, string Password);
    public record LoginDto(string Username, string Password);
    public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
    public record UploadImageDto(Guid? UploadedBy = null);
    public record TextFileDto(Guid Id, string FileName, string Path, Guid ImageId, DateTime CreatedAt);
    public class RefreshRequestDto { public string RefreshToken { get; set; } }
    public sealed class RerunOcrRequest { public string Language { get; set; } = "eng"; public string Model { get; set; } }
    public record UpdateTextDto(string Content);
    public sealed class OcrSummaryDto
    {
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Processing { get; set; }
        public int Failed { get; set; }
    }

}