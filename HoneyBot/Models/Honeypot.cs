namespace HoneyBot.Models
{
    public class Honeypot
    {
        public int Id { get; set; }
        public string Name { get; set; }      // e.g., "WordPress Login"
        public string UrlPath { get; set; }   // e.g., "/wp-login.php"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
