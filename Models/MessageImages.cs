namespace DGN_BACK.Models
{
    public class MessageImage
    {
        public int Id { get; set; }
        
        public int MessageId { get; set; }
        public virtual Message Message { get; set; } = null!;

        public int ImageId { get; set; }
        public virtual Image Image { get; set; } = null!;
    }
} 