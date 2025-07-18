namespace DGN_BACK.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string GondericiAdi { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<MessageImage> MessageImages { get; set; } = new List<MessageImage>();
    }
} 