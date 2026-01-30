using System.Net.Http;
using System.Text.Json;
using backend.OcrModels.Helpers;
using backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;

public class PaddleOcrService : IOcrService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    public PaddleOcrService(
        HttpClient http,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        _http = http;
        _config = config;
        _env = env;
        _http.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<string> ExtractTextAsync(string imagePath, string? language)
    {
        var lang = OcrLanguage.NormalizeForPaddle(language ?? "eng");

        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Image file not found", imagePath);

        using var content = new MultipartFormDataContent();
        await using var fs = File.OpenRead(imagePath);

        content.Add(new StreamContent(fs), "file", Path.GetFileName(imagePath));
        content.Add(new StringContent(lang), "lang");

        // 🔑 IMPORTANT: disable angle classifier (fixes the 500 error)
        content.Add(new StringContent("false"), "cls");

        var baseUrl = _config["PaddleOCR:BaseUrl"]
            ?? throw new InvalidOperationException("PaddleOCR:BaseUrl missing");

        var res = await _http.PostAsync($"{baseUrl}/ocr", content);
        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"PaddleOCR {(int)res.StatusCode}: {body}"
            );

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("text").GetString() ?? "";
    }

    public async Task<string> ExtractTextToFileAsync(string imagePath, string outputFile, string? language)
    {
        var text = await ExtractTextAsync(imagePath, language);
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
        await File.WriteAllTextAsync(outputFile, text);
        return text;
    }
}
