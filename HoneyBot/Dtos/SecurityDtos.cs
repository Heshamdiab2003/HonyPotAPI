namespace HoneyBot.Dtos
{

    public class SecurityProfileDto
    {
        public int Id { get; set; }
        public string IpAddress { get; set; }
        public string? FingerprintHash { get; set; }
        public int ReputationScore { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class BlockIpRequest
    {
        public string IpAddress { get; set; }
        public int DurationInHours { get; set; } = 24; // المدة الافتراضية 24 ساعة
        public string Reason { get; set; }
    }
}
