using Document_Storage_System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Document_Storage_System.Controllers
{
    [Authorize]
    [Route("api/libraries")]
    [ApiController]
    public class LibrariesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LibrariesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/libraries
        [HttpGet]
        public async Task<IActionResult> GetLibraries()
        {
            var libraries = await _context.Libraries
                .Select(lib => new
                {
                    lib.LibraryId,
                    lib.Name,
                    Department = lib.Department.Name
                })
                .ToListAsync();

            return Ok(libraries);
        }

        // GET: api/libraries/{libraryId}/topics
        [HttpGet("{libraryId}/topics")]
        public async Task<IActionResult> GetTopicsInLibrary(int libraryId)
        {
            var topics = await _context.Topics
                .Where(t => t.LibraryId == libraryId)
                .OrderBy(t => t.Path)
                .Select(t => new
                {
                    t.TopicId,
                    t.Name,
                    t.Path,
                    t.ParentTopicId
                })
                .ToListAsync();

            return Ok(topics);
        }
    }
}
