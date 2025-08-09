using System.ComponentModel.DataAnnotations;

namespace Document_Storage_System.Models.Entities
{
    public class LibraryPermission
    {
        [Key]
        public int PermissionId { get; set; }

        public string UserId { get; set; }
        public int LibraryId { get; set; }

        public bool CanRead { get; set; } = true;
        public bool CanWrite { get; set; } = false;
        public bool CanDelete { get; set; } = false;
        public DateTime GrantedAt { get; set; } = DateTime.Now;

        public AppUser User { get; set; }
        public Library Library { get; set; }
    }

}
