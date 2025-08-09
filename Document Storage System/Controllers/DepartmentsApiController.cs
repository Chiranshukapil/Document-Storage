using Document_Storage_System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Storage_System.Controllers
{
    [Authorize]
    [Route("api/departments")]
    [ApiController]
    public class DepartmentsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/departments
        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _context.Departments
                .Select(d => new
                {
                    d.DepartmentId,
                    d.Name
                })
                .ToListAsync();

            return Ok(departments);
        }

        // GET: api/departments/{id}/libraries
        [HttpGet("{id}/libraries")]
        public async Task<IActionResult> GetLibrariesInDepartment(int id)
        {
            var libraries = await _context.Libraries
                .Where(lib => lib.DepartmentId == id)
                .Select(lib => new
                {
                    lib.LibraryId,
                    lib.Name
                })
                .ToListAsync();

            return Ok(libraries);
        }
    }
}
