using Document_Storage_System.Data;
using Document_Storage_System.Models;
using Document_Storage_System.Models.Entities;
using Document_Storage_System.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Storage_System.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public DocumentController(ApplicationDbContext context, UserManager<AppUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // Show documents under a topic
        public async Task<IActionResult> Index(int topicId, string? search)
        {
            var topic = await _context.Topics
                .Include(t => t.Library)
                .FirstOrDefaultAsync(t => t.TopicId == topicId);

            if (topic == null || !await HasLibraryAccess(topic.LibraryId))
                return Forbid();

            var query = _context.Documents
                .Include(d => d.UploadedBy)
                .Where(d => d.TopicId == topicId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d =>
                    d.Title.Contains(search) ||
                    d.FileName.Contains(search) ||
                    d.UploadedBy.UserName.Contains(search));
            }

            var documents = await query.ToListAsync();

            ViewBag.Topic = topic;
            ViewBag.LibraryId = topic.LibraryId;
            ViewBag.Search = search;

            return View(documents);
        }

        // Upload GET
        public async Task<IActionResult> Upload(int topicId)
        {
            var topic = await _context.Topics
                .Include(t => t.Library)
                .FirstOrDefaultAsync(t => t.TopicId == topicId);

            if (topic == null || !await HasLibraryAccess(topic.LibraryId))
                return Forbid();

            ViewBag.Topic = topic;
            return View(new DocumentUploadViewModel { TopicId = topicId });
        }

        // Upload POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(DocumentUploadViewModel model, [FromServices] IConfiguration config)
        {
            var topic = await _context.Topics
                .Include(t => t.Library)
                .FirstOrDefaultAsync(t => t.TopicId == model.TopicId);

            if (topic == null || !await HasLibraryAccess(topic.LibraryId))
                return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.Topic = topic;
                return View(model);
            }

            // Read file restrictions from appsettings.json
            var allowedExtensions = config.GetSection("FileStorage:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>();
            var maxFileSize = config.GetValue<long>("FileStorage:MaxFileSize");
            var basePath = config.GetValue<string>("FileStorage:BasePath") ?? Path.Combine(_env.WebRootPath, "uploads");

            // Check extension
            var extension = Path.GetExtension(model.UploadedFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("UploadedFile", $"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}");
                ViewBag.Topic = topic;
                return View(model);
            }

            // Check size
            if (model.UploadedFile.Length > maxFileSize)
            {
                ModelState.AddModelError("UploadedFile", $"File size exceeds the maximum limit of {maxFileSize / (1024 * 1024)} MB.");
                ViewBag.Topic = topic;
                return View(model);
            }

            // Ensure storage directory exists
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            // Save file
            var uniqueFileName = Guid.NewGuid() + extension;
            var fullPath = Path.Combine(basePath, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await model.UploadedFile.CopyToAsync(stream);
            }

            var user = await _userManager.GetUserAsync(User);

            var doc = new Document
            {
                Title = model.Title,
                TopicId = model.TopicId,
                FileName = model.UploadedFile.FileName,
                FilePath = fullPath, // store absolute or relative depending on your needs
                FileSize = model.UploadedFile.Length,
                ContentType = model.UploadedFile.ContentType,
                UploadedById = user.Id,
                CreatedAt = DateTime.Now
            };

            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { topicId = model.TopicId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var doc = await _context.Documents
                .Include(d => d.Topic)
                .ThenInclude(t => t.Library)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (doc == null || !await HasLibraryAccess(doc.Topic.LibraryId))
                return Forbid();

            var fullPath = Path.Combine(_env.WebRootPath, doc.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            int topicId = doc.TopicId;
            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { topicId });
        }

        public async Task<IActionResult> Details(int id)
        {
            var doc = await _context.Documents
                .Include(d => d.UploadedBy)
                .Include(d => d.Topic)
                    .ThenInclude(t => t.Library)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (doc == null || !await HasLibraryAccess(doc.Topic.LibraryId))
                return Forbid();

            return View(doc);
        }


        // ✅ Now allows Admins by default
        private async Task<bool> HasLibraryAccess(int libraryId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return true;

            return await _context.LibraryPermissions
                .AnyAsync(lp => lp.LibraryId == libraryId && lp.UserId == user.Id);
        }
    }
}
