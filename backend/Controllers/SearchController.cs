using backend.Database;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private const int ContextLength = 120;
        private readonly AppDbContext _db;
        private readonly ILogger<SearchController> _logger;
        private readonly IRateLimiterService _rateLimiter;

        public SearchController(AppDbContext db, ILogger<SearchController> logger, IRateLimiterService rateLimiter)
        {
            _db = db;
            _logger = logger;
            _rateLimiter = rateLimiter;
        }

        // ===================== SEARCH DOCUMENT =====================
        [HttpGet("document/{documentId:guid}")]
        public async Task<IActionResult> SearchDocument(
            Guid documentId,
            [FromQuery] string q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest("Search query is required");

            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            if (!_rateLimiter.CanPerformAction(userId, "search"))
                return StatusCode(StatusCodes.Status429TooManyRequests, "Too many searches. Slow down.");

            _rateLimiter.RecordAction(userId, "search");

            q = q.Trim();

            var textFilesQuery = _db.TextFiles
                .AsNoTracking()
                .Include(t => t.Image)
                    .ThenInclude(i => i.Section)
                        .ThenInclude(s => s.Document)
                .Where(t =>
                    t.Image.Section.DocumentId == documentId &&
                    t.Image.Section.Document.CreatedById == userId &&
                    t.Image.OcrProcessed)
                .OrderBy(t => t.Image.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var textFiles = await textFilesQuery
                .Select(t => new
                {
                    t.Path,
                    t.ImageId,
                    ImageName = t.Image.FileName,
                    SectionTitle = t.Image.Section.Title,
                    DocumentTitle = t.Image.Section.Document.Title
                })
                .ToListAsync();

            var results = new List<object>();

            foreach (var tf in textFiles)
            {
                var snippets = await FindSnippetsAsync(tf.Path, q);
                if (snippets.Count > 0)
                {
                    results.Add(new
                    {
                        tf.ImageId,
                        ImageName = tf.ImageName ?? "(unknown)",
                        SectionTitle = tf.SectionTitle ?? "(unknown)",
                        DocumentTitle = tf.DocumentTitle ?? "(unknown)",
                        Snippets = snippets,
                        PreviewUrl = $"/api/image/raw/{tf.ImageId}"
                    });
                }
            }

            return Ok(new
            {
                Query = q,
                Count = results.Count,
                Page = page,
                PageSize = pageSize,
                Results = results
            });
        }

        // ===================== STREAM FILE & FIND MULTIPLE SNIPPETS =====================
        private async Task<List<string>> FindSnippetsAsync(string path, string query)
        {
            var snippets = new List<string>();
            if (!System.IO.File.Exists(path)) return snippets;

            try
            {
                using var reader = new StreamReader(path, Encoding.UTF8);
                var buffer = new char[4096]; // read in small chunks
                var sb = new StringBuilder();
                int read;

                while ((read = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    sb.Append(buffer, 0, read);

                    var content = sb.ToString();
                    var matches = Regex.Matches(content, Regex.Escape(query), RegexOptions.IgnoreCase);

                    foreach (Match match in matches)
                    {
                        snippets.Add(ExtractSnippet(content, match.Index));
                    }

                    // Keep only last 2*ContextLength chars for streaming efficiency
                    if (sb.Length > ContextLength * 2)
                        sb.Remove(0, sb.Length - ContextLength);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read file {Path}", path);
            }

            return snippets;
        }

        // ===================== EXTRACT SNIPPET & HIGHLIGHT =====================
        private static string ExtractSnippet(string text, int matchIndex, int context = ContextLength)
        {
            var start = Math.Max(0, matchIndex - context / 2);
            var length = Math.Min(context, text.Length - start);
            var snippet = text.Substring(start, length)
                              .Replace("\n", " ")
                              .Replace("\r", " ")
                              .Trim();

            // Highlight match with <mark>
            var relativeIndex = matchIndex - start;
            if (relativeIndex >= 0 && relativeIndex < snippet.Length)
            {
                snippet = snippet.Substring(0, relativeIndex)
                          + "<mark>"
                          + snippet.Substring(relativeIndex, Math.Min(context / 4, snippet.Length - relativeIndex))
                          + "</mark>"
                          + snippet.Substring(Math.Min(relativeIndex + context / 4, snippet.Length));
            }

            return snippet;
        }

        private Guid GetUserId()
        {
            var claim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }
}
