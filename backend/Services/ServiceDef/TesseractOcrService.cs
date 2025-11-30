using System.Diagnostics;
using backend.Services.Interfaces;

namespace backend.Services.ServiceDef;

public class TesseractOcrService : ITesseractOcrService
{
    private readonly string _tessDataPath;
    private readonly string _language;

    public TesseractOcrService(IConfiguration config)
    {
        // Use the path from appsettings.json
        _tessDataPath = config["Tesseract:TessdataPath"] 
                        ?? throw new Exception("TessdataPath not configured");
        _language = config["Tesseract:Language"] ?? "eng";
    }

    public async Task<string> ExtractTextAsync(string imagePath)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Image file not found.", imagePath);

        string tempOutput = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        var startInfo = new ProcessStartInfo
        {
            FileName = "tesseract",
            Arguments = $"\"{imagePath}\" \"{tempOutput}\" -l {_language}",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Use path from appsettings.json
        startInfo.Environment["TESSDATA_PREFIX"] = _tessDataPath;

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        string stderr = await process.StandardError.ReadToEndAsync();
        string stdout = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
            throw new Exception($"Tesseract failed (ExitCode={process.ExitCode}): {stderr}");

        string resultFile = tempOutput + ".txt";
        if (!File.Exists(resultFile))
            throw new Exception("Tesseract did not produce output file.");

        string result = await File.ReadAllTextAsync(resultFile);
        File.Delete(resultFile);

        return result;
    }

    public async Task<string> ExtractTextToFileAsync(string imagePath, string outputFile)
    {
        var text = await ExtractTextAsync(imagePath);
        await File.WriteAllTextAsync(outputFile, text);
        return text;
    }
}
