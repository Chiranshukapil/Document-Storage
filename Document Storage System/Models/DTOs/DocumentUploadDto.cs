using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Document_Storage_System.Models.DTOs
{
    public class DocumentUploadDto
    {
        [Required]
        public int TopicId { get; set; }

        [Required]
        public string Title { get; set; } = "";

        [Required]
        public IFormFile File { get; set; } = null!;
    }
}
