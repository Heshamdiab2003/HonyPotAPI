namespace HoneyBot.Models
{
    public class IpBlock
    {
        public int Id { get; set; }
        public string IpAddress { get; set; }
        public string Reason { get; set; }
        public DateTime ExpiresAt { get; set; } // When the block ends
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
