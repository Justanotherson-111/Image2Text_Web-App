using System.Diagnostics;
using backend.Services.Interfaces;
using backend.OcrModels.Helpers;

namespace backend.Services.ServiceDef;

public class TesseractOcrService : IOcrService
{
    private readonly string _tessDataPath;
    private readonly string _defaultLanguage;

    public TesseractOcrService(IConfiguration config)
    {
        _tessDataPath = config["Tesseract:TessdataPath"]
            ?? throw new InvalidOperationException("Tesseract:TessdataPath not configured");

        _defaultLanguage = config["Tesseract:Language"] ?? "eng";
    }

    public async Task<string> ExtractTextAsync(string imagePath, string? language)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Image file not found.", imagePath);

        var lang = OcrLanguage.NormalizeForTesseract(
            string.IsNullOrWhiteSpace(language)
                ? _defaultLanguage
                : language.Trim()
        );

        var tempOutputBase = Path.Combine(
            Path.GetTempPath(),
            Path.GetRandomFileName()
        );

        var startInfo = new ProcessStartInfo
        {
            FileName = "tesseract",
            Arguments = $"\"{imagePath}\" \"{tempOutputBase}\" -l {lang}",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.Environment["TESSDATA_PREFIX"] = _tessDataPath;

        using var process = Process.Start(startInfo)
            ?? throw new Exception("Failed to start tesseract process");

        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new Exception($"Tesseract OCR failed: {stderr}");

        var resultFile = tempOutputBase + ".txt";
        if (!File.Exists(resultFile))
            throw new Exception("Tesseract did not generate output file.");

        try
        {
            return await File.ReadAllTextAsync(resultFile);
        }
        finally
        {
            File.Delete(resultFile);
        }
    }

    public async Task<string> ExtractTextToFileAsync(string imagePath, string outputFile, string? language)
    {
        var text = await ExtractTextAsync(imagePath, language);
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
        await File.WriteAllTextAsync(outputFile, text);
        return text;
    }
}
