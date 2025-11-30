namespace backend.Services.Interfaces
{
    public interface IOcrService
    {
        Task<string> ExtractTextAsync(string imagePath);
    }
}