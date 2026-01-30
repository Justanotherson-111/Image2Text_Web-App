using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using backend.Database;
using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet("ocr-summary")]
    public async Task<ActionResult<OcrSummaryDto>> GetOcrSummary([FromQuery] Guid documentId)
    {
        if (documentId == Guid.Empty)
            return BadRequest(new { error = "Invalid documentId." });

        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        // Use EF aggregation to avoid pulling all rows
        var summary = await _db.OcrJobs
            .Where(j => j.Image.Section.DocumentId == documentId && j.Image.Section.Document.CreatedById == userId)
            .GroupBy(j => 1)
            .Select(g => new OcrSummaryDto
            {
                Total = g.Count(),
                Completed = g.Count(j => j.Status == OcrJobStatus.Completed),
                Processing = g.Count(j => j.Status == OcrJobStatus.Running),
                Failed = g.Count(j => j.Status == OcrJobStatus.Failed)
            })
            .FirstOrDefaultAsync();

        // Return empty summary if no jobs
        summary ??= new OcrSummaryDto();

        return Ok(summary);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                    User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

