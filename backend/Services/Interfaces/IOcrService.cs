namespace backend.Services.Interfaces;

public interface IOcrService
{
    Task<string> ExtractTextAsync(string imagePath, string? language);
    Task<string> ExtractTextToFileAsync(string imagePath, string outputFile, string? language);
}

