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
    [Authorize(Roles = "Admin")]
    public class LibraryPermissionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LibraryPermissionController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int libraryId)
        {
            var library = await _context.Libraries
                .Include(l => l.Department)
                .FirstOrDefaultAsync(l => l.LibraryId == libraryId);
            if (library == null) return NotFound();

            var usersInDept = await _context.Users
                .Where(u => u.DepartmentId == library.DepartmentId)
                .ToListAsync();

            var permissions = await _context.LibraryPermissions
                .Where(p => p.LibraryId == libraryId)
                .ToListAsync();

            var model = usersInDept.Select(user =>
            {
                var perm = permissions.FirstOrDefault(p => p.UserId == user.Id);
                return new LibraryPermissionViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    LibraryId = libraryId,
                    IsAdmin = _userManager.IsInRoleAsync(user, "Admin").Result,
                    CanRead = perm?.CanRead ?? false,
                    CanWrite = perm?.CanWrite ?? false,
                    CanDelete = perm?.CanDelete ?? false
                };
            }).ToList();

            ViewBag.LibraryName = library.Name;
            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> Grant(string userId, int libraryId)
        {
            if (!_context.LibraryPermissions.Any(p => p.UserId == userId && p.LibraryId == libraryId))
            {
                _context.LibraryPermissions.Add(new LibraryPermission
                {
                    UserId = userId,
                    LibraryId = libraryId
                });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", new { libraryId });
        }

        [HttpPost]
        public async Task<IActionResult> Revoke(string userId, int libraryId)
        {
            var permission = await _context.LibraryPermissions
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LibraryId == libraryId);
            if (permission != null)
            {
                _context.LibraryPermissions.Remove(permission);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", new { libraryId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePermission(string userId, int libraryId, bool canRead, bool canWrite, bool canDelete)
        {
            var permission = await _context.LibraryPermissions
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LibraryId == libraryId);

            if (permission == null)
            {
                permission = new LibraryPermission
                {
                    UserId = userId,
                    LibraryId = libraryId,
                    GrantedAt = DateTime.Now
                };
                _context.LibraryPermissions.Add(permission);
            }

            permission.CanRead = canRead;
            permission.CanWrite = canWrite;
            permission.CanDelete = canDelete;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", new { libraryId });
        }

    }
}
