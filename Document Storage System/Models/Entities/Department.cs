using System.ComponentModel.DataAnnotations;

namespace Document_Storage_System.Models.Entities
{
    public class Department
    {
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public ICollection<Library> Libraries { get; set; } = new List<Library>();
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    }
}
