namespace HoneyBot.Dtos
{
    // نموذج لعرض أخطر IPs
    public class DangerousIpDto
    {
        public string IpAddress { get; set; }
        public int ReputationScore { get; set; }
        public DateTime LastSeen { get; set; }
    }

}
