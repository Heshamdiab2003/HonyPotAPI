namespace HoneyBot.Models;

public class Incident
{
    public int Id { get; set; }
    public int HoneypotId { get; set; }
    public Honeypot Honeypot { get; set; }

    public string AttackerIp { get; set; }
    public string AttackTypeGuess { get; set; }
    public DateTime IncidentTime { get; set; } = DateTime.UtcNow;

    public string UserAgent { get; set; }
    public string Country { get; set; }
    public string City { get; set; }
    public double? Latitude { get; set; }  // <-- إضافة جديدة
    public double? Longitude { get; set; } // <-- إضافة جديدة

    public ICollection<RequestLog> RequestLogs { get; set; }
}