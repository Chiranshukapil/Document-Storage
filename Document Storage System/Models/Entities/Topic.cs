using System.ComponentModel.DataAnnotations;

namespace Document_Storage_System.Models.Entities
{
    public class Topic
    {
        public int TopicId { get; set; }

        public int LibraryId { get; set; }
        public int? ParentTopicId { get; set; }

        [Required(ErrorMessage = "Topic name is required")]
        public string Name { get; set; }

        public string? Path { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Library? Library { get; set; }
        public Topic? ParentTopic { get; set; }

        public ICollection<Topic> SubTopics { get; set; } = new List<Topic>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
