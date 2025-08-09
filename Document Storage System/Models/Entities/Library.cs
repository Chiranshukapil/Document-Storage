using System.ComponentModel.DataAnnotations;

namespace Document_Storage_System.Models.Entities
{
    public class Library
    {
        public int LibraryId { get; set; }

        [Required(ErrorMessage = "Please select a department")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Library name is required")]
        public string Name { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public Department? Department { get; set; }

        public ICollection<Topic>? Topics { get; set; }
    }
}
