namespace HoneyBot.Models
{
    public class SecurityProfile
    {
        public int Id { get; set; }
        public string IpAddress { get; set; } // سنحتفظ به كمعلومة إضافية

        // ==> الإضافة الأهم <==
        public string? FingerprintHash { get; set; } // لتخزين بصمة المتصفح

        public int ReputationScore { get; set; } = 100;
        public DateTime LastSeen { get; set; }
        public string? Notes { get; set; }
    }
}
