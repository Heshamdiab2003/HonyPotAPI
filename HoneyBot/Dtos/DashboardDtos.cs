using HoneyBot.Dtos;
namespace HoneyBot.Dros
{
    public class DashboardSummaryDto
    {
        public long TotalIncidents { get; set; }
        public long IncidentsLast24Hours { get; set; }
        public IEnumerable<DangerousIpDto> TopDangerousIps { get; set; }
        public IEnumerable<AttackTypeDistributionDto> AttackTypeDistribution { get; set; }
    }

    
  
}
