using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Document_Storage_System.Models.ViewModels
{
    public class DocumentUploadViewModel
    {
        public int TopicId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please select a file to upload.")]
        public IFormFile UploadedFile { get; set; }
    }
}
