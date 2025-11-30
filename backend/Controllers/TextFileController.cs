using backend.Database;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/textfile")]
public class TextFileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IRateLimiterService _rateLimiter;
    public TextFileController(AppDbContext db, IRateLimiterService rateLimiterService)
    {
        _db = db;
        _rateLimiter = rateLimiterService;
    }


    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ListFiles()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userIdClaim == null) return Unauthorized("User claim missing.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid user claim.");

        IQueryable<TextFile> query;

        if (User.IsInRole("Admin"))
        {
            query = _db.TextFiles;
        }
        else
        {
            query = _db.TextFiles.Where(t => t.CreatedById == userId);
        }

        var list = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.FileName,
                t.ImageId,
                t.CreatedAt,
                CreatedById = t.CreatedById,
                TextStatus = System.IO.File.Exists(t.Path) ? "Available" : "[No text extracted]"  // <- new
            })
            .ToListAsync();

        return Ok(list);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> DownloadFile(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userIdClaim == null) return Unauthorized("User claim missing.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid user claim.");

        if (!_rateLimiter.CanPerformAction(userId, "download"))
            return BadRequest("Rate limit exceeded.");

        var tf = await _db.TextFiles.FindAsync(id);
        if (tf == null) return NotFound();

        if (tf.CreatedById != null && tf.CreatedById != userId && !User.IsInRole("Admin"))
            return Forbid();

        // If the text file is missing, create a placeholder
        if (!System.IO.File.Exists(tf.Path))
        {
            var placeholderPath = Path.Combine(Path.GetDirectoryName(tf.Path) ?? "/tmp", tf.Id + "_placeholder.txt");
            await System.IO.File.WriteAllTextAsync(placeholderPath, "[No text extracted]");
            tf.Path = placeholderPath;

            // Optional: save updated path to DB
            _db.TextFiles.Update(tf);
            await _db.SaveChangesAsync();
        }
        _rateLimiter.RecordAction(userId, "download");
        var bytes = await System.IO.File.ReadAllBytesAsync(tf.Path);
        return File(bytes, "text/plain", tf.FileName);
    }
}
