using Document_Storage_System.Data;
using Document_Storage_System.Models;
using Document_Storage_System.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Storage_System.Controllers
{
    [Authorize]
    public class TopicController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public TopicController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int libraryId, string? search)
        {
            if (!await HasLibraryAccess(libraryId)) return Forbid();

            var query = _context.Topics
                .Include(t => t.ParentTopic)
                .Where(t => t.LibraryId == libraryId);

            if (!string.IsNullOrEmpty(search))
            {
                string lowerSearch = search.ToLower();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(lowerSearch) ||
                    t.Path.ToLower().Contains(lowerSearch)
                );
            }

            var topics = await query.OrderBy(t => t.Path).ToListAsync();

            ViewBag.LibraryId = libraryId;
            ViewBag.LibraryName = (await _context.Libraries.FindAsync(libraryId))?.Name ?? "Unknown Library";
            ViewBag.Search = search;

            // ✅ Do not reset TempData here – just let it flow to the view
            return View(topics);
        }

        public async Task<IActionResult> Create(int libraryId)
        {
            if (!await HasLibraryWriteAccess(libraryId)) return Forbid();

            ViewBag.LibraryId = libraryId;
            ViewBag.Topics = await _context.Topics
                .Where(t => t.LibraryId == libraryId)
                .OrderBy(t => t.Path)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Topic topic)
        {
            if (!await HasLibraryWriteAccess(topic.LibraryId)) return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.LibraryId = topic.LibraryId;
                ViewBag.Topics = await _context.Topics
                    .Where(t => t.LibraryId == topic.LibraryId)
                    .OrderBy(t => t.Path)
                    .ToListAsync();
                return View(topic);
            }

            topic.CreatedAt = DateTime.Now;
            topic.Path = topic.ParentTopicId != null
                ? $"{(await _context.Topics.FindAsync(topic.ParentTopicId))?.Path}/{topic.Name}"
                : topic.Name;

            _context.Topics.Add(topic);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Topic '{topic.Name}' created successfully!";
            return RedirectToAction("Index", new { libraryId = topic.LibraryId });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var topic = await _context.Topics.FindAsync(id);
            if (topic == null || !await HasLibraryWriteAccess(topic.LibraryId)) return NotFound();

            ViewBag.LibraryId = topic.LibraryId;
            ViewBag.Topics = await _context.Topics
                .Where(t => t.LibraryId == topic.LibraryId && t.TopicId != id)
                .ToListAsync();

            return View(topic);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Topic updated)
        {
            if (id != updated.TopicId) return BadRequest();

            var topic = await _context.Topics.FindAsync(id);
            if (topic == null || !await HasLibraryWriteAccess(topic.LibraryId)) return NotFound();

            bool nameChanged = topic.Name != updated.Name;
            bool parentChanged = topic.ParentTopicId != updated.ParentTopicId;

            topic.Name = updated.Name;
            topic.ParentTopicId = updated.ParentTopicId;
            topic.Path = topic.ParentTopicId != null
                ? $"{(await _context.Topics.FindAsync(topic.ParentTopicId))?.Path}/{topic.Name}"
                : topic.Name;

            if (nameChanged || parentChanged)
            {
                await UpdateChildPathsRecursive(topic.TopicId, topic.Path);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Topic '{topic.Name}' updated successfully!";
            return RedirectToAction("Index", new { libraryId = topic.LibraryId });
        }

        private async Task UpdateChildPathsRecursive(int parentId, string parentPath)
        {
            var children = await _context.Topics
                .Where(t => t.ParentTopicId == parentId)
                .ToListAsync();

            foreach (var child in children)
            {
                child.Path = $"{parentPath}/{child.Name}";
                await UpdateChildPathsRecursive(child.TopicId, child.Path);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var topic = await _context.Topics.Include(t => t.ParentTopic).FirstOrDefaultAsync(t => t.TopicId == id);
            if (topic == null || !await HasLibraryWriteAccess(topic.LibraryId)) return NotFound();

            ViewBag.HasChildren = await _context.Topics.AnyAsync(t => t.ParentTopicId == id);
            return View(topic);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var topic = await _context.Topics.FindAsync(id);
            if (topic == null || !await HasLibraryWriteAccess(topic.LibraryId)) return NotFound();

            bool hasChildren = await _context.Topics.AnyAsync(t => t.ParentTopicId == id);
            bool hasDocuments = await _context.Documents.AnyAsync(d => d.TopicId == id);

            if (hasChildren || hasDocuments)
            {
                TempData["ErrorMessage"] = hasChildren
                    ? "Cannot delete a topic that has child topics."
                    : "Cannot delete a topic that contains documents.";

                return RedirectToAction("Index", new { libraryId = topic.LibraryId });
            }

            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Topic '{topic.Name}' deleted successfully!";
            return RedirectToAction("Index", new { libraryId = topic.LibraryId });
        }

        public async Task<IActionResult> GetTopicHierarchy(int libraryId)
        {
            if (!await HasLibraryAccess(libraryId)) return Forbid();

            var topics = await _context.Topics
                .Where(t => t.LibraryId == libraryId)
                .OrderBy(t => t.Path)
                .Select(t => new { t.TopicId, t.Name, t.Path, t.ParentTopicId })
                .ToListAsync();

            return Json(topics);
        }

        public IActionResult TopicHierarchy(int libraryId)
        {
            ViewBag.LibraryId = libraryId;
            return View();
        }

        private async Task<bool> HasLibraryAccess(int libraryId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (await _userManager.IsInRoleAsync(user, "Admin")) return true;

            return await _context.LibraryPermissions
                .AnyAsync(lp => lp.LibraryId == libraryId && lp.UserId == user.Id);
        }

        private async Task<bool> HasLibraryWriteAccess(int libraryId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (await _userManager.IsInRoleAsync(user, "Admin")) return true;

            return await _context.LibraryPermissions
                .AnyAsync(lp => lp.LibraryId == libraryId && lp.UserId == user.Id && lp.CanWrite);
        }
    }
}
