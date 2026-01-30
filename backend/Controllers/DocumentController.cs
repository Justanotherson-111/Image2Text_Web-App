using backend.Models;
using backend.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/document")]
    public class DocumentController : ControllerBase
    {
        private readonly AppDbContext _db;
        public DocumentController(AppDbContext db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDocumentDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var doc = new Document { Title = dto.Title, CreatedById = userId };
            _db.Documents.Add(doc);
            await _db.SaveChangesAsync();

            return Ok(new { doc.Id, doc.Title, doc.CreatedAt });
        }

        [HttpPost("{documentId:guid}/section")]
        public async Task<IActionResult> CreateSection(Guid documentId, [FromBody] CreateSectionDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var doc = await _db.Documents.FirstOrDefaultAsync(d => d.Id == documentId && d.CreatedById == userId);
            if (doc == null) return NotFound(new { error = "Document not found" });

            // Use max OrderIndex + 1 instead of Count to avoid concurrency issues
            var maxIndex = await _db.DocumentSections
                .Where(s => s.DocumentId == documentId)
                .MaxAsync(s => (int?)s.OrderIndex) ?? -1;

            var section = new DocumentSection
            {
                DocumentId = documentId,
                Title = dto.Title,
                OrderIndex = maxIndex + 1
            };

            _db.DocumentSections.Add(section);
            await _db.SaveChangesAsync();

            return Ok(new SectionResponseDto
            {
                Id = section.Id,
                Title = section.Title,
                OrderIndex = section.OrderIndex
            });
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var docs = await _db.Documents
                .Where(d => d.CreatedById == userId)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.CreatedAt,
                    Sections = d.Sections.OrderBy(s => s.OrderIndex)
                        .Select(s => new { s.Id, s.Title })
                })
                .ToListAsync();

            return Ok(docs);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var doc = await _db.Documents
                .Include(d => d.Sections)
                    .ThenInclude(s => s.Images)
                .FirstOrDefaultAsync(d => d.Id == id && d.CreatedById == userId);

            if (doc == null) return NotFound(new { error = "Document not found" });

            return Ok(doc);
        }

        [HttpDelete("section/{sectionId:guid}")]
        public async Task<IActionResult> DeleteSection(Guid sectionId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var section = await _db.DocumentSections
                .Include(s => s.Images)
                    .ThenInclude(i => i.TextFiles)
                .Include(s => s.Document)
                .FirstOrDefaultAsync(s => s.Id == sectionId);

            if (section == null) return NotFound(new { error = "Section not found" });
            if (section.Document.CreatedById != userId) return Forbid();

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Delete files from disk
                foreach (var img in section.Images)
                {
                    foreach (var tf in img.TextFiles)
                        if (System.IO.File.Exists(tf.Path)) System.IO.File.Delete(tf.Path);

                    if (System.IO.File.Exists(img.Path)) System.IO.File.Delete(img.Path);
                }

                // Remove DB entries
                _db.TextFiles.RemoveRange(section.Images.SelectMany(i => i.TextFiles));
                _db.Images.RemoveRange(section.Images);
                _db.DocumentSections.Remove(section);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { message = "Section deleted successfully" });
            }
            catch
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = "Failed to delete section" });
            }
        }

        [HttpDelete("{documentId:guid}")]
        public async Task<IActionResult> DeleteDocument(Guid documentId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var doc = await _db.Documents
                .Include(d => d.Sections)
                    .ThenInclude(s => s.Images)
                        .ThenInclude(i => i.TextFiles)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatedById == userId);

            if (doc == null) return NotFound(new { error = "Document not found" });

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var section in doc.Sections)
                {
                    foreach (var img in section.Images)
                    {
                        foreach (var tf in img.TextFiles)
                            if (System.IO.File.Exists(tf.Path)) System.IO.File.Delete(tf.Path);

                        if (System.IO.File.Exists(img.Path)) System.IO.File.Delete(img.Path);
                    }

                    _db.TextFiles.RemoveRange(section.Images.SelectMany(i => i.TextFiles));
                    _db.Images.RemoveRange(section.Images);
                }

                _db.DocumentSections.RemoveRange(doc.Sections);
                _db.Documents.Remove(doc);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { message = "Document deleted successfully" });
            }
            catch
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { error = "Failed to delete document" });
            }
        }

        // ================== HELPERS ==================
        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }

        public class CreateDocumentDto { public string Title { get; set; } = null!; }
        public class CreateSectionDto { public string Title { get; set; } = null!; }
        public class SectionResponseDto
        {
            public Guid Id { get; set; }
            public string Title { get; set; } = null!;
            public int OrderIndex { get; set; }
        }
    }
}