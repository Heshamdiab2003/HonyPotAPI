namespace HoneyBot.Dtos;

// لعرض ملخص الحادثة في القائمة
public class IncidentSummaryDto
{
    public int Id { get; set; }
    public string AttackerIp { get; set; }
    public string AttackTypeGuess { get; set; }
    public DateTime IncidentTime { get; set; }
    public string Country { get; set; }
}

// لعرض التفاصيل الكاملة للحادثة
public class IncidentDetailDto : IncidentSummaryDto
{
    public string UserAgent { get; set; }
    public string City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public IEnumerable<RequestLogDto> RequestLogs { get; set; }
}

// لعرض تفاصيل الطلب
public class RequestLogDto
{
    public string Method { get; set; }
    public string FullUrl { get; set; }
    public string Headers { get; set; }
    public string Body { get; set; }
}