using Document_Storage_System.Data;
using Document_Storage_System.Models;
using Document_Storage_System.Models.DTOs;
using Document_Storage_System.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Storage_System.Controllers
{
    [Authorize]
    [Route("api/documents")]
    [ApiController]
    public class DocumentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<AppUser> _userManager;

        public DocumentsApiController(ApplicationDbContext context, IWebHostEnvironment env, UserManager<AppUser> userManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
        }

        // GET: api/documents/by-topic/5
        [HttpGet("by-topic/{topicId}")]
        public async Task<IActionResult> GetDocumentsByTopic(int topicId)
        {
            var docs = await _context.Documents
                .Where(d => d.TopicId == topicId)
                .Select(d => new
                {
                    d.DocumentId,
                    d.Title,
                    d.FileName,
                    d.FilePath,
                    d.FileSize,
                    d.ContentType,
                    d.CreatedAt
                })
                .ToListAsync();

            return Ok(docs);
        }

        // GET: api/documents/details/5
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetDocumentDetails(int id)
        {
            var doc = await _context.Documents
                .Include(d => d.UploadedBy)
                .Where(d => d.DocumentId == id)
                .Select(d => new
                {
                    d.DocumentId,
                    d.Title,
                    d.FileName,
                    d.FilePath,
                    d.FileSize,
                    d.ContentType,
                    d.CreatedAt,
                    UploadedBy = d.UploadedBy.UserName
                })
                .FirstOrDefaultAsync();

            if (doc == null)
                return NotFound();

            return Ok(doc);
        }

        // POST: api/documents/upload
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] DocumentUploadDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var topic = await _context.Topics.FindAsync(dto.TopicId);
            if (topic == null)
                return BadRequest("Invalid Topic ID");

            var file = dto.File;
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var fullPath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var user = await _userManager.GetUserAsync(User);

            var document = new Document
            {
                Title = dto.Title,
                TopicId = dto.TopicId,
                FileName = file.FileName,
                FilePath = "/uploads/" + fileName,
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedById = user?.Id ?? "unknown",
                CreatedAt = DateTime.Now
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Uploaded successfully", document.DocumentId });
        }

        // DELETE: api/documents/delete/5
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null)
                return NotFound();

            var fullPath = Path.Combine(_env.WebRootPath, doc.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Document deleted", documentId = id });
        }

        // GET: api/documents/download/5
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null)
                return NotFound();

            var fullPath = Path.Combine(_env.WebRootPath, doc.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (!System.IO.File.Exists(fullPath))
                return NotFound("File not found on disk.");

            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return File(fileStream, doc.ContentType, doc.FileName);
        }
    }
}
