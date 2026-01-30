using backend.Database;
using backend.DTOs;
using backend.Models;
using backend.OcrModels.Helpers;
using backend.Services.Interfaces;
using backend.Services.ServiceDef;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/image")]
public class ImageController : ControllerBase
{
    private const int PreviewLength = 100;

    private readonly IImageService _imageService;
    private readonly IBackgroundTaskQueue _queue;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ImageController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebHostEnvironment _env;
    private readonly IRateLimiterService _rateLimiter;
    public ImageController(
        IImageService imageService,
        IBackgroundTaskQueue queue,
        AppDbContext db,
        IConfiguration config,
        ILogger<ImageController> logger,
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment environment,
        IRateLimiterService rateLimiter)
    {
        _imageService = imageService;
        _queue = queue;
        _db = db;
        _config = config;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _env = environment;
        _rateLimiter = rateLimiter;
    }

    // ===================== UPLOAD =====================
    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] List<IFormFile> files, [FromForm] Guid sectionId, [FromForm] string language, [FromForm] string model)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        if (!_rateLimiter.CanPerformAction(userId, "image-upload"))
            return StatusCode(429, new { error = "Please wait before uploading more images." });

        _rateLimiter.RecordAction(userId, "image-upload");

        var section = await _db.DocumentSections
            .Include(s => s.Document)
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        if (section == null) return NotFound("Section not found");
        if (section.Document.CreatedById != userId) return Forbid();

        // Validate language early
        try
        {
            _ = model == "paddleocr"
                ? OcrLanguage.NormalizeForPaddle(language)
                : OcrLanguage.NormalizeForTesseract(language);
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }

        var result = new List<object>();

        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            var image = await _imageService.SaveImageAsync(
                stream,
                file.FileName,
                sectionId,
                userId
            );

            _ = _queue.Enqueue(() =>
                ProcessOcrAsync(image.Id, image.Path, userId, language, model)
            );

            result.Add(new
            {
                image.Id,
                image.FileName,
                image.UploadedAt
            });
        }

        return Ok(result);
    }
    // ===================== LIST BY SECTION =====================
    [HttpGet("section/{sectionId:guid}")]
    public async Task<IActionResult> ListBySection(Guid sectionId, int page = 1, int pageSize = 20)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var section = await _db.DocumentSections.Include(s => s.Document).FirstOrDefaultAsync(s => s.Id == sectionId);
        if (section == null) return NotFound();
        if (section.Document.CreatedById != userId) return Forbid();

        var imagesQuery = _db.Images
            .Include(i => i.TextFiles)
            .Where(i => i.SectionId == sectionId)
            .OrderBy(i => i.UploadedAt)
            .AsNoTracking();

        var total = await imagesQuery.CountAsync();
        var images = await imagesQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var dtos = new List<object>();
        foreach (var img in images)
        {
            dtos.Add(new
            {
                img.Id,
                img.FileName,
                img.UploadedAt,
                img.OcrProcessed,
                PreviewText = await ReadPreviewTextAsync(img.TextFiles.FirstOrDefault()?.Path ?? ""),
                PreviewUrl = $"/api/image/raw/{img.Id}"
            });
        }

        return Ok(new { total, page, pageSize, images = dtos });
    }

    // ===================== DELETE IMAGE =====================
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();

        var image = await _db.Images
            .Include(i => i.Section)
            .ThenInclude(s => s.Document)
            .Include(i => i.TextFiles)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (image == null)
            return NotFound();

        if (image.Section.Document.CreatedById != userId)
            return Forbid();

        await DeleteImageInternalAsync(image);
        await _db.SaveChangesAsync();

        return Ok();
    }
    // =============== DELETE ALL IMG FROM SECTION =====================
    [HttpDelete("section/{sectionId:guid}")]
    public async Task<IActionResult> DeleteSectionImages(Guid sectionId)
    {
        var userId = GetUserId();
        var section = await _db.DocumentSections
            .Include(s => s.Images)
            .ThenInclude(i => i.TextFiles)
            .Include(s => s.Document)
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        if (section == null) return NotFound();
        if (section.Document.CreatedById != userId) return Forbid();

        foreach (var img in section.Images)
        {
            foreach (var tf in img.TextFiles)
                if (System.IO.File.Exists(tf.Path)) System.IO.File.Delete(tf.Path);

            if (System.IO.File.Exists(img.Path)) System.IO.File.Delete(img.Path);
        }

        _db.Images.RemoveRange(section.Images);
        await _db.SaveChangesAsync();

        return Ok();
    }

    // ===================== RAW IMAGE =====================
    [HttpGet("raw/{id:guid}")]
    public async Task<IActionResult> GetRaw(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var image = await _db.Images
            .Include(i => i.Section)
            .ThenInclude(s => s.Document)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (image == null) return NotFound();
        if (image.Section.Document.CreatedById != userId) return Forbid();

        var fullPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, image.Path));
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        // Determine content type more robustly
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };

        // Use PhysicalFile for efficient streaming
        return PhysicalFile(fullPath, contentType, Path.GetFileName(fullPath));
    }
    // =============== GET OCR PROCESS ===============
    [HttpGet("ocr-jobs/by-image/{imageId:guid}")]
    public async Task<IActionResult> GetOcrJob(Guid imageId)
    {
        var userId = GetUserId();

        if (!_rateLimiter.CanPerformAction(userId, "ocr-status"))
            return StatusCode(StatusCodes.Status429TooManyRequests, "Please wait before re-running OCR.");

        _rateLimiter.RecordAction(userId, "ocr-status");

        var job = await _db.OcrJobs
        .Include(j => j.Image)
        .ThenInclude(i => i.Section)
        .ThenInclude(s => s.Document)
        .Where(j =>
            j.ImageId == imageId &&
            j.Image.Section.Document.CreatedById == userId
        )
        .Select(j => new
        {
            status = j.Status.ToString(),
            progress = j.Progress,
            errorCode = j.ErrorCode,
            errorMessage = j.ErrorMessage,
            language = j.Language
        })
        .FirstOrDefaultAsync();

        if (job == null) return NotFound();
        return Ok(job);
    }
    // ================= Re-Run OCR ==================
    [HttpPost("{id:guid}/rerun-ocr")]
    public async Task<IActionResult> RerunOcr(Guid id, [FromBody] RerunOcrRequest req)
    {
        var userId = GetUserId();

        var image = await _db.Images
            .Include(i => i.TextFiles)
            .Include(i => i.OcrJob)
            .FirstOrDefaultAsync(i => i.Id == id && i.UploadedById == userId);

        if (image == null)
            return NotFound("Image not found");

        if (image.OcrJob?.Status == OcrJobStatus.Running)
            return BadRequest("OCR is currently running");

        // Delete old OCR results
        foreach (var tf in image.TextFiles)
            SafeDeleteFile(tf.Path);

        _db.TextFiles.RemoveRange(image.TextFiles);
        image.TextFiles.Clear();
        image.OcrProcessed = false;

        if (image.OcrJob == null)
        {
            image.OcrJob = new OcrJob
            {
                ImageId = image.Id,
                Status = OcrJobStatus.Pending,
                Progress = 0
            };
            _db.OcrJobs.Add(image.OcrJob);
        }
        else
        {
            image.OcrJob.Status = OcrJobStatus.Pending;
            image.OcrJob.Progress = 0;
            image.OcrJob.ErrorMessage = null;
            image.OcrJob.ErrorCode = null;
            image.OcrJob.StartedAt = null;
            image.OcrJob.CompletedAt = null;
        }

        await _db.SaveChangesAsync();

        // Resolve model + language
        var model = string.IsNullOrWhiteSpace(req.Model)
            ? _config["OCR:DefaultModel"] ?? "tesseract"
            : req.Model.Trim().ToLowerInvariant();

        var rawLang = string.IsNullOrWhiteSpace(req.Language)
            ? _config["OCR:DefaultLanguage"] ?? "eng"
            : req.Language.Trim();



        // Enqueue OCR
        _ = _queue.Enqueue(() =>
            ProcessOcrAsync(
                image.Id,
                image.Path,
                userId,
                rawLang,
                model
            )
        );

        return Ok(new
        {
            message = "OCR re-run started",
            imageId = image.Id,
            model,
            language = rawLang
        });
    }
    // ===================== OCR =====================
    private async Task ProcessOcrAsync(Guid imageId, string imagePath, Guid userId, string language, string model)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ImageController>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        // Normalize language (single source of truth)
        string lang;
        try
        {
            lang = model.Equals("paddleocr", StringComparison.OrdinalIgnoreCase)
                ? OcrLanguage.NormalizeForPaddle(language)
                : OcrLanguage.NormalizeForTesseract(language);
        }
        catch (ArgumentException)
        {
            await MarkJobFailedAsync(
                db, imageId,
                OcrErrorCodes.UnsupportedLanguage,
                $"OCR language '{language}' is not supported."
            );
            return;
        }

        // Resolve OCR engine
        IOcrService ocr = model.Equals("paddleocr", StringComparison.OrdinalIgnoreCase)
            ? scope.ServiceProvider.GetRequiredService<PaddleOcrService>()
            : scope.ServiceProvider.GetRequiredService<TesseractOcrService>();

        // Normalize image path
        var fullImagePath = Path.IsPathRooted(imagePath)
            ? imagePath
            : Path.GetFullPath(Path.Combine(env.ContentRootPath, imagePath));

        if (!System.IO.File.Exists(fullImagePath))
        {
            await MarkJobFailedAsync(
                db, imageId,
                OcrErrorCodes.FileNotFound,
                "Image file was not found."
            );
            return;
        }

        // Load image + ensure SINGLE OCR job
        var image = await db.Images
            .Include(i => i.OcrJob)
            .FirstOrDefaultAsync(i => i.Id == imageId);

        if (image == null)
            return;

        var job = await db.OcrJobs
            .FirstOrDefaultAsync(j => j.ImageId == imageId);

        if (job == null)
        {
            job = new OcrJob { ImageId = imageId };
            db.OcrJobs.Add(job);
        }

        image.OcrJob = job;

        // Reset job state
        job.Status = OcrJobStatus.Running;
        job.Progress = 0;
        job.ErrorCode = null;
        job.ErrorMessage = null;
        job.StartedAt = DateTime.UtcNow;
        job.CompletedAt = null;
        job.Language = lang;

        image.OcrProcessed = false;

        await db.SaveChangesAsync();

        // Output file
        var textDir = config["Storage:TextPath"] ?? "/app/ExtractedText";
        Directory.CreateDirectory(textDir);
        var outFile = Path.Combine(textDir, $"{imageId}.txt");

        try
        {
            job.Progress = 20;
            await db.SaveChangesAsync();

            var rawText = await ocr.ExtractTextAsync(fullImagePath, lang);

            string finalText = rawText;

            if (ShouldApplyCorrection(model, lang))
            {
                var corrector = scope.ServiceProvider.GetRequiredService<ICorrector>();

                try
                {
                    finalText = await corrector.CorrectAsync(rawText, lang);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "mT5 correction failed, using raw OCR text");
                    finalText = rawText;
                }
            }

            await System.IO.File.WriteAllTextAsync(outFile, finalText);

            job.Progress = 70;
            await db.SaveChangesAsync();

            db.TextFiles.Add(new TextFile
            {
                FileName = Path.GetFileName(outFile),
                Path = outFile,
                ImageId = imageId,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            });

            job.Progress = 90;
            image.OcrProcessed = true;
            await db.SaveChangesAsync();

            job.Progress = 100;
            job.Status = OcrJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "PaddleOCR HTTP failure for image {ImageId}", imageId);

            job.Status = OcrJobStatus.Failed;
            job.ErrorCode = OcrErrorCodes.EngineFailure;
            job.ErrorMessage = "PaddleOCR service is unavailable or failed.";
            job.CompletedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError(ex, "OCR file missing for image {ImageId}", imageId);

            job.Status = OcrJobStatus.Failed;
            job.ErrorCode = OcrErrorCodes.FileNotFound;
            job.ErrorMessage = "OCR input or output file was missing.";
            job.CompletedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OCR engine failure for image {ImageId}", imageId);

            job.Status = OcrJobStatus.Failed;
            job.ErrorCode = model.Equals("paddleocr", StringComparison.OrdinalIgnoreCase)
                ? OcrErrorCodes.EngineFailure
                : OcrErrorCodes.MissingTessdata;

            job.ErrorMessage = "OCR engine failed unexpectedly.";
            job.CompletedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }
    }
    private async Task DeleteImageInternalAsync(Image image)
    {
        try
        {
            foreach (var tf in image.TextFiles)
                SafeDeleteFile(tf.Path);

            SafeDeleteFile(image.Path);

            _db.TextFiles.RemoveRange(image.TextFiles);
            _db.Images.Remove(image);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file {Path}", image.Path);
        }
    }

    // private static object BuildImageDto(Image img)
    // {
    //     var preview = "";

    //     var tf = img.TextFiles.FirstOrDefault();
    //     if (tf != null && System.IO.File.Exists(tf.Path))
    //     {
    //         using var reader = new StreamReader(tf.Path);
    //         var buffer = new char[PreviewLength];
    //         var read = reader.Read(buffer, 0, buffer.Length);
    //         preview = new string(buffer, 0, read);
    //     }

    //     return new
    //     {
    //         img.Id,
    //         img.FileName,
    //         img.UploadedAt,
    //         img.OcrProcessed,
    //         PreviewText = preview,
    //         PreviewUrl = $"/api/image/raw/{img.Id}"
    //     };
    // }
    private void SafeDeleteFile(string path)
    {
        try
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
        catch { }
    }

    private Guid GetUserId()
    {
        var claim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
    private async Task<string> ReadPreviewTextAsync(string path, int length = PreviewLength)
    {
        if (!System.IO.File.Exists(path)) return string.Empty;

        try
        {
            using var reader = new StreamReader(path);
            var buffer = new char[length];
            var read = await reader.ReadAsync(buffer, 0, length);
            return new string(buffer, 0, read);
        }
        catch
        {
            return string.Empty;
        }
    }
    private static async Task MarkJobFailedAsync(AppDbContext db, Guid imageId, string errorCode, string message)
    {
        var job = await db.OcrJobs.FirstOrDefaultAsync(j => j.ImageId == imageId);
        if (job == null) return;

        job.Status = OcrJobStatus.Failed;
        job.ErrorCode = errorCode;
        job.ErrorMessage = message;
        job.CompletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }
    private static bool ShouldApplyCorrection(string model, string lang)
    {
        if (model.Equals("paddleocr", StringComparison.OrdinalIgnoreCase))
            return true;

        return lang.StartsWith("vi") || lang.StartsWith("ja") || lang.StartsWith("ko");
    }

}
