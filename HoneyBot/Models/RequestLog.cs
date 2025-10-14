namespace HoneyBot.Models
{
    public class RequestLog
    {
        public int Id { get; set; }
        public int IncidentId { get; set; } // FK to Incident
        public Incident Incident { get; set; }

        public string Method { get; set; } // "GET", "POST"
        public string FullUrl { get; set; }
        public string Headers { get; set; } // Store as JSON string
        public string Body { get; set; }
    }
}
