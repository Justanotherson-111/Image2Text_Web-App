using backend.Database;
using backend.Hubs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/image")]
public class ImageController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceProvider _provider;
    private readonly AppDbContext _db;
    private readonly IRateLimiterService _rateLimiter;
    private readonly IConfiguration _config;
    private readonly ILogger<ImageController> _logger;
    private readonly IHubContext<OcrHub> _ocrHub;

    public ImageController(
        IImageService imageService,
        IBackgroundTaskQueue queue,
        IServiceProvider provider,
        AppDbContext db,
        IRateLimiterService rateLimiter,
        IConfiguration config,
        ILogger<ImageController> logger,
        IHubContext<OcrHub> hub)
    {
        _imageService = imageService;
        _queue = queue;
        _provider = provider;
        _db = db;
        _rateLimiter = rateLimiter;
        _config = config;
        _logger = logger;
        _ocrHub = hub;
    }
    // ===================== UPLOAD =====================
    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
    {
        var userId = GetUserId();

        if (!_rateLimiter.CanPerformAction(userId, "upload"))
            return BadRequest("Rate limit exceeded.");

        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        Image image;
        using (var stream = file.OpenReadStream())
            image = await _imageService.SaveImageAsync(stream, file.FileName, userId);

        var imageId = image.Id.ToString();

        await SendProgress(imageId, 25);

        _queue.Enqueue(async () =>
        {
            using var scope = _provider.CreateScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ocr = scope.ServiceProvider.GetRequiredService<ITesseractOcrService>();

            try
            {
                var textPath = _config["Storage:TextPath"] ?? "/app/ExtractedText";
                Directory.CreateDirectory(textPath);
                var outFile = Path.Combine(textPath, image.Id + ".txt");

                var job = new OcrJob
                {
                    ImageId = image.Id,
                    Completed = false,
                    CreatedAt = DateTime.UtcNow
                };

                scopedDb.OcrJobs.Add(job);

                string text;
                try
                {
                    text = await ocr.ExtractTextToFileAsync(image.Path, outFile);
                    await SendProgress(imageId, 50);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OCR failed for image {ImageId}", image.Id);
                    text = "[OCR ERROR]";
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    text = "[No text extracted]";
                    await System.IO.File.WriteAllTextAsync(outFile, text);
                    await SendProgress(imageId, 75);
                }

                scopedDb.TextFiles.Add(new TextFile
                {
                    FileName = Path.GetFileName(outFile),
                    Path = outFile,
                    ImageId = image.Id,
                    CreatedById = image.UploadedById,
                    CreatedAt = DateTime.UtcNow
                });

                job.Completed = true;
                job.ResultPath = outFile;
                job.CompletedAt = DateTime.UtcNow;

                var dbImage = await scopedDb.Images.FindAsync(image.Id);
                if (dbImage != null)
                    dbImage.OcrProcessed = true;

                await scopedDb.SaveChangesAsync();

                await SendProgress(imageId, 100);
                await SendCompleted(imageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal OCR background error for image {ImageId}", image.Id);
            }
        });

        _rateLimiter.RecordAction(userId, "upload");

        return Ok(new
        {
            image.Id,
            image.FileName,
            image.OcrProcessed
        });
    }
    // ===================== LIST =====================
    [HttpGet("list")]
    public async Task<IActionResult> ListImages()
    {
        var userId = GetUserId();

        IQueryable<Image> query = _db.Images.AsNoTracking();

        if (!User.IsInRole("Admin"))
            query = query.Where(i => i.UploadedById == userId);

        var list = await query
            .OrderByDescending(i => i.UploadedAt)
            .Select(i => new
            {
                i.Id,
                i.FileName,
                i.OcrProcessed,
                i.UploadedById,
                i.UploadedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteImage(Guid id)
    {
        var userId = GetUserId();

        var image = await _db.Images.FirstOrDefaultAsync(i => i.Id == id);
        if (image == null)
            return NotFound("Image not found.");

        // Non-admin users can delete only their own images
        if (!User.IsInRole("Admin") && image.UploadedById != userId)
            return Forbid("You are not allowed to delete this image.");

        // Delete OCR text file if exists
        var textFile = await _db.TextFiles.FirstOrDefaultAsync(t => t.ImageId == id);
        if (textFile != null && System.IO.File.Exists(textFile.Path))
        {
            System.IO.File.Delete(textFile.Path);
            _db.TextFiles.Remove(textFile);
        }

        // Delete image file from disk
        if (System.IO.File.Exists(image.Path))
            System.IO.File.Delete(image.Path);

        // Remove image from DB
        _db.Images.Remove(image);

        await _db.SaveChangesAsync();

        return Ok(new { message = "Image deleted successfully" });
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user id claim.");

        return userId;
    }
    private Task SendProgress(string imageId, int progress)
    {
        return _ocrHub.Clients.Group(imageId)
            .SendAsync("OcrProgress", new { imageId, progress });
    }

    private Task SendCompleted(string imageId)
    {
        return _ocrHub.Clients.Group(imageId)
            .SendAsync("OcrCompleted", new { imageId, completed = true });
    }

}
