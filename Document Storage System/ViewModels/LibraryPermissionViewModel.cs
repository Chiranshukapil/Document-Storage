namespace Document_Storage_System.Models.ViewModels
{
    public class LibraryPermissionViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int LibraryId { get; set; }

        public bool IsAdmin { get; set; }

        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanDelete { get; set; }
    }


}
