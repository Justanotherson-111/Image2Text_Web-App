using backend.Database;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.ServiceDef;

public class ImageService : IImageService
{
    private readonly string _uploadsDir;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ImageService> _logger;

    public ImageService(IConfiguration config, AppDbContext db, ILogger<ImageService> logger)
    {
        _logger = logger;
        _dbContext = db;
        _uploadsDir = config["Storage:ImagesPath"] ?? "/app/Uploads";
        if (!Directory.Exists(_uploadsDir)) Directory.CreateDirectory(_uploadsDir);
    }

    public async Task<Image> SaveImageAsync(Stream stream, string fileName, Guid? uploadedById = null)
    {
        var saveName = $"{Guid.NewGuid()}_{fileName}";
        var path = Path.Combine(_uploadsDir, saveName);
        await using var fs = File.Create(path);
        await stream.CopyToAsync(fs);

        var image = new Image { FileName = fileName, Path = path, UploadedById = uploadedById };
        _dbContext.Images.Add(image);
        await _dbContext.SaveChangesAsync();
        return image;
    }

    public async Task<bool> DeleteImageAsync(Guid id)
    {
        var image = await _dbContext.Images
            .Include(i => i.OcrJob)
            .Include(i => i.TextFiles)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (image == null) return false;
        if (!File.Exists(image.Path))
            _logger.LogWarning("File {Path} not found on disk.", image.Path);

        if (image.OcrJob?.ResultPath != null && File.Exists(image.OcrJob.ResultPath))
            File.Delete(image.OcrJob.ResultPath);

        foreach (var tf in image.TextFiles ?? Enumerable.Empty<TextFile>())
            if (File.Exists(tf.Path)) File.Delete(tf.Path);

        if (File.Exists(image.Path)) File.Delete(image.Path);

        _dbContext.Images.Remove(image);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
