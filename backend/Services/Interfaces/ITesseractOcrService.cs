namespace backend.Services.Interfaces
{
    public interface ITesseractOcrService
    {
        Task<string> ExtractTextAsync(string imagePath);
        Task<string> ExtractTextToFileAsync(string imagePath, string outputFile);
    }
}
