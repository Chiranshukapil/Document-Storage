namespace Document_Storage_System.Models.Entities
{
    public class Document
    {
        public int DocumentId { get; set; }
        public int TopicId { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public string UploadedById { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Topic Topic { get; set; }
        public AppUser UploadedBy { get; set; }
    }
}
