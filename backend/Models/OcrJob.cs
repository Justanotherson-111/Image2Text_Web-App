namespace backend.Models
{
    public class OcrJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ImageId { get; set; }
        public Image Image { get; set; } = null!;

        public OcrJobStatus Status { get; set; } = OcrJobStatus.Pending;

        public int Progress { get; set; } = 0;

        public string? Language { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public enum OcrJobStatus
    {
        Pending,
        Running,
        Completed,
        Failed
    }
    public static class OcrErrorCodes
    {
        public const string UnsupportedLanguage = "OCR_UNSUPPORTED_LANGUAGE";
        public const string MissingTessdata = "OCR_MISSING_TESSDATA";
        public const string EngineFailure = "OCR_ENGINE_FAILURE";
        public const string FileNotFound = "OCR_IMAGE_NOT_FOUND";
    }
}
