using Document_Storage_System.Data;
using Document_Storage_System.Models;
using Document_Storage_System.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Storage_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public UserController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /User/AssignDepartment
        public async Task<IActionResult> AssignDepartment()
        {
            var usersWithoutDept = await _context.Users
                .Where(u => u.DepartmentId == 0 || u.DepartmentId == null)
                .ToListAsync();

            var departments = await _context.Departments.ToListAsync();

            ViewBag.Departments = departments;
            return View(usersWithoutDept);
        }

        // POST: /User/AssignDepartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDepartment(string userId, int departmentId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.DepartmentId = departmentId;
            await _context.SaveChangesAsync();

            return RedirectToAction("AssignDepartment");
        }
    }
}
