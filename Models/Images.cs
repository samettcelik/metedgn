namespace DGN_BACK.Models
{
    public class Image
    {
        public int Id { get; set; }
        public string PublicId { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        public string OptimizedUrl { get; set; } = string.Empty;
        public string CroppedUrl { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<MessageImage> MessageImages { get; set; } = new List<MessageImage>();
    }
} 