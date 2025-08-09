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
    public class LibraryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LibraryController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            // Admins see all libraries
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var all = await _context.Libraries.Include(l => l.Department).ToListAsync();
                return View(all);
            }

            // Regular users see only libraries they have access to
            var accessibleIds = await _context.LibraryPermissions
                .Where(lp => lp.UserId == user.Id && lp.CanRead)
                .Select(lp => lp.LibraryId)
                .ToListAsync();

            var libraries = await _context.Libraries
                .Include(l => l.Department)
                .Where(l => accessibleIds.Contains(l.LibraryId))
                .ToListAsync();

            return View(libraries);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Library library)
        {
            var exists = await _context.Libraries
                .AnyAsync(l => l.DepartmentId == library.DepartmentId);

            if (exists)
            {
                ModelState.AddModelError("DepartmentId", "This department already has a library.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ValidationErrors = ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .Select(e => $"❌ {e.Key}: {e.Value.Errors[0].ErrorMessage}")
                    .ToList();

                ViewBag.Departments = await _context.Departments.ToListAsync();
                return View(library);
            }

            library.CreatedAt = DateTime.Now;
            _context.Libraries.Add(library);
            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            _context.LibraryPermissions.Add(new LibraryPermission
            {
                LibraryId = library.LibraryId,
                UserId = user.Id,
                CanRead = true,
                CanWrite = true,
                CanDelete = true
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Library '{library.Name}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var library = await _context.Libraries.FindAsync(id);
            if (library == null)
                return NotFound();

            if (!await CanWriteLibrary(id))
                return Forbid();

            return View(library);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Library updated)
        {
            if (id != updated.LibraryId)
                return BadRequest();

            var library = await _context.Libraries.FindAsync(id);
            if (library == null)
                return NotFound();

            if (!await CanWriteLibrary(id))
                return Forbid();

            library.Name = updated.Name;
            library.Description = updated.Description;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var library = await _context.Libraries.FindAsync(id);
            if (library == null)
                return NotFound();

            if (!await CanWriteLibrary(id))
                return Forbid();

            return View(library);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var library = await _context.Libraries.FindAsync(id);
            if (library == null)
                return NotFound();

            if (!await CanWriteLibrary(id))
                return Forbid();

            _context.Libraries.Remove(library);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ✅ Permission helpers
        private async Task<bool> CanReadLibrary(int libraryId)
        {
            var user = await _userManager.GetUserAsync(User);
            return await _context.LibraryPermissions
                .AnyAsync(lp => lp.LibraryId == libraryId && lp.UserId == user.Id && lp.CanRead)
                || await _userManager.IsInRoleAsync(user, "Admin");
        }

        private async Task<bool> CanWriteLibrary(int libraryId)
        {
            var user = await _userManager.GetUserAsync(User);
            return await _context.LibraryPermissions
                .AnyAsync(lp => lp.LibraryId == libraryId && lp.UserId == user.Id && lp.CanWrite)
                || await _userManager.IsInRoleAsync(user, "Admin");
        }
    }
}
