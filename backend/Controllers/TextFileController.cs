using backend.Database;
using backend.DTOs;
using backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Document = QuestPDF.Fluent.Document;

namespace backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/textfile")]
    public class TextFileController : ControllerBase
    {
        private const int PreviewLength = 200;
        private readonly AppDbContext _db;

        public TextFileController(AppDbContext db)
        {
            _db = db;
        }

        // ===================== LIST BY DOCUMENT (+ OPTIONAL SECTION) =====================
        [HttpGet("document/{documentId:guid}")]
        public async Task<IActionResult> ListByDocument(Guid documentId, [FromQuery] Guid? sectionId, int page = 1, int pageSize = 20)
        {
            var userId = GetUserId();

            var query = _db.TextFiles
                .AsNoTracking()
                .Include(t => t.Image)
                    .ThenInclude(i => i.Section)
                .Where(t => t.Image.Section.DocumentId == documentId && t.Image.Section.Document.CreatedById == userId);

            if (sectionId.HasValue)
                query = query.Where(t => t.Image.SectionId == sectionId.Value);

            var total = await query.CountAsync();

            var files = await query
                .OrderBy(t => t.Image.Section.OrderIndex)
                .ThenBy(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(tf => new
                {
                    tf.Id,
                    tf.FileName,
                    tf.CreatedAt,
                    SectionTitle = tf.Image.Section.Title,
                    PreviewText = SafeReadPreview(tf.Path)
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, files });
        }

        // ===================== DOWNLOAD =====================
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Download(Guid id)
        {
            var userId = GetUserId();
            var tf = await GetTextFileWithOwnershipAsync(id, userId);
            if (tf == null) return NotFound();

            var stream = new FileStream(tf.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, "text/plain", tf.FileName);
        }
        // ===================== COMBINE ALL TEXTS INTO ONE DOCUMENT =====================
        [HttpGet("document/{documentId:guid}/combine")]
        public async Task<IActionResult> CombineAndDownload(Guid documentId)
        {
            var userId = GetUserId();

            var document = await _db.Documents
                .Include(d => d.Sections)
                    .ThenInclude(s => s.Images)
                        .ThenInclude(i => i.TextFiles)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatedById == userId);

            if (document == null)
                return NotFound();

            byte[] bytes;

            using (var memStream = new MemoryStream())
            {
                using (var wordDoc = WordprocessingDocument.Create(
                    memStream,
                    WordprocessingDocumentType.Document,
                    true))
                {
                    var mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(
                        new Body()
                    );

                    var body = mainPart.Document.Body;

                    // ===================== TITLE =====================
                    var titlePara = new Paragraph();
                    titlePara.ParagraphProperties = new ParagraphProperties(
                        new Justification { Val = JustificationValues.Center }
                    );

                    titlePara.Append(
                        new Run(
                            new RunProperties(
                                new Bold(),
                                new FontSize { Val = "32" }
                            ),
                            new Text(SanitizeForOpenXml(document.Title ?? "Untitled Document"))
                        )
                    );

                    body.Append(titlePara);
                    body.Append(new Paragraph()); // spacing

                    // ===================== SECTIONS =====================
                    foreach (var section in document.Sections.OrderBy(s => s.OrderIndex))
                    {
                        var sectionHeader = new Paragraph();
                        sectionHeader.ParagraphProperties = new ParagraphProperties(
                            new SpacingBetweenLines { After = "200" }
                        );

                        sectionHeader.Append(
                            new Run(
                                new RunProperties(new Bold()),
                                new Text(SanitizeForOpenXml(
                                    $"Section {section.OrderIndex}: {section.Title ?? "Untitled"}"
                                ))
                            )
                        );

                        body.Append(sectionHeader);

                        foreach (var tf in section.Images
                            .SelectMany(i => i.TextFiles)
                            .OrderBy(t => t.CreatedAt))
                        {
                            if (!System.IO.File.Exists(tf.Path))
                                continue;

                            using var reader = new StreamReader(tf.Path);
                            string? line;

                            while ((line = await reader.ReadLineAsync()) != null)
                            {
                                var textPara = new Paragraph();
                                textPara.Append(
                                    new Run(
                                        new Text(SanitizeForOpenXml(line))
                                        {
                                            Space = SpaceProcessingModeValues.Preserve
                                        }
                                    )
                                );

                                body.Append(textPara);
                            }
                        }

                        body.Append(new Paragraph()); // section spacing
                    }

                    mainPart.Document.Save();
                }

                // MUST extract bytes after disposing WordprocessingDocument
                bytes = memStream.ToArray();
            }

            var fileName = string.IsNullOrWhiteSpace(document.Title)
                ? "document.docx"
                : $"{SafeFileName(document.Title)}.docx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName
            );
        }
        // ===================== COMBINE AND DOWNLOAD AS PDF ==================
        [HttpGet("document/{documentId:guid}/combine-pdf")]
        public async Task<IActionResult> CombineAndDownloadPdf(Guid documentId)
        {
            var userId = GetUserId();

            var document = await _db.Documents
                .Include(d => d.Sections)
                    .ThenInclude(s => s.Images)
                        .ThenInclude(i => i.TextFiles)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == documentId && d.CreatedById == userId);

            if (document == null)
                return NotFound();

            var pdfBytes = GeneratePdf(document);

            var fileName = string.IsNullOrWhiteSpace(document.Title)
                ? "document.pdf"
                : $"{SafeFileName(document.Title)}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        // ===================== GET CONTENT (START EDIT) =====================
        [HttpGet("{id:guid}/content")]
        public async Task<IActionResult> GetContent(Guid id)
        {
            var userId = GetUserId();
            var tf = await GetTextFileWithOwnershipAsync(id, userId);
            if (tf == null) return NotFound();
            if (!System.IO.File.Exists(tf.Path)) return NotFound("Text file missing");

            var content = await System.IO.File.ReadAllTextAsync(tf.Path);
            return Ok(new { content, tf.IsManuallyEdited });
        }
        // ===================== UPDATE CONTENT (END EDIT) =====================
        [HttpPut("{id:guid}/content")]
        public async Task<IActionResult> UpdateContent(Guid id, [FromBody] UpdateTextDto dto)
        {
            var userId = GetUserId();
            var tf = await GetTextFileWithOwnershipAsync(id, userId);
            if (tf == null) return NotFound();

            await System.IO.File.WriteAllTextAsync(tf.Path, dto.Content);
            tf.IsManuallyEdited = true;
            tf.EditedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return NoContent();
        }
        // ===================== HELPERS =====================
        private static string SafeReadPreview(string path)
        {
            try
            {
                if (!System.IO.File.Exists(path)) return "";

                using var reader = new StreamReader(path);
                var buffer = new char[PreviewLength];
                var read = reader.Read(buffer, 0, buffer.Length);
                return new string(buffer, 0, read);
            }
            catch
            {
                return "";
            }
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
        private static string SanitizeForOpenXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            return new string(text
                .Where(c =>
                    c == 0x9 || c == 0xA || c == 0xD ||
                    (c >= 0x20 && c <= 0xD7FF) ||
                    (c >= 0xE000 && c <= 0xFFFD))
                .ToArray());
        }
        string SafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
        private async Task<TextFile?> GetTextFileWithOwnershipAsync(Guid textFileId, Guid userId)
        {
            return await _db.TextFiles
                .Include(t => t.Image)
                    .ThenInclude(i => i.Section)
                        .ThenInclude(s => s.Document)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == textFileId && t.Image.Section.Document.CreatedById == userId);
        }
        private byte[] GeneratePdf(backend.Models.Document document)
        {
            var sections = document.Sections.OrderBy(s => s.OrderIndex).Select(s => new
            {
                s.OrderIndex,
                Title = s.Title ?? "Untitled",
                TextBlocks = s.Images
                .SelectMany(i => i.TextFiles)
                .OrderBy(t => t.CreatedAt)
                .Where(t => System.IO.File.Exists(t.Path))
                .Select(t => System.IO.File.ReadAllText(t.Path))
                .ToList()
            }).ToList();
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    page.Content().Column(col =>
                    {
                        col.Item()
                            .AlignCenter()
                            .Text(document.Title ?? "Untitled Document")
                            .FontSize(20)
                            .Bold();

                        col.Item().PaddingVertical(10).LineHorizontal(1);
                        foreach (var section in sections)
                        {
                            col.Item()
                               .PaddingTop(15)
                               .Text($"Section {section.OrderIndex}: {section.Title}")
                               .FontSize(14)
                               .Bold();

                            foreach (var text in section.TextBlocks)
                            {
                                col.Item()
                                   .PaddingBottom(8)
                                   .Text(text)
                                   .FontSize(11)
                                   .LineHeight(1.4f);
                            }
                        }
                    });
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated on ");
                            x.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm"))
                             .SemiBold();
                        });
                });
            }).GeneratePdf();
        }
    }
}
