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

        if (!Directory.Exists(_uploadsDir))
            Directory.CreateDirectory(_uploadsDir);
    }

    /// <summary>
    /// Save an uploaded image and link it to a DocumentSection
    /// </summary>
    public async Task<Image> SaveImageAsync(Stream stream, string fileName, Guid sectionId, Guid? uploadedById = null)
    {
        // Check if section exists
        var section = await _dbContext.DocumentSections
            .Include(s => s.Document)
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        if (section == null)
            throw new ArgumentException("Section not found", nameof(sectionId));

        // Save file to disk
        var saveName = $"{Guid.NewGuid()}_{fileName}";
        var path = Path.Combine(_uploadsDir, saveName);

        await using var fs = File.Create(path);
        await stream.CopyToAsync(fs);

        // Create DB record
        var image = new Image
        {
            FileName = fileName,
            Path = path,
            UploadedById = uploadedById,
            SectionId = sectionId,
            UploadedAt = DateTime.UtcNow,
            OcrProcessed = false
        };

        _dbContext.Images.Add(image);
        await _dbContext.SaveChangesAsync();

        return image;
    }
}
