// --- 1. كل الـ using statements المطلوبة ---

using HoneyBot.Dros;
using HoneyBot.Dtos;
using HoneyBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoneyBot.Controllers
{
    [ApiController]
    // المسار الأساسي لكل endpoints في هذا الكنترولر سيكون api/dashboard
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =================================================================
        // == 1. قسم ملخص الإحصائيات (من DashboardController الأصلي)
        // =================================================================
        [HttpGet("summary")] // GET /api/dashboard/summary
        public async Task<IActionResult> GetDashboardSummary()
        {
            var totalIncidents = await _context.Incidents.LongCountAsync();
            var incidentsLast24Hours = await _context.Incidents
                .CountAsync(i => i.IncidentTime >= DateTime.UtcNow.AddHours(-24));

            var topDangerousIps = await _context.SecurityProfiles
                .OrderBy(p => p.ReputationScore).Take(5)
                .Select(p => new DangerousIpDto
                {
                    IpAddress = p.IpAddress,
                    ReputationScore = p.ReputationScore,
                    LastSeen = p.LastSeen
                }).ToListAsync();

            var attackTypeDistribution = new List<AttackTypeDistributionDto>();
            if (totalIncidents > 0)
            {
                attackTypeDistribution = await _context.Incidents
                   .GroupBy(i => i.AttackTypeGuess)
                   .Select(g => new AttackTypeDistributionDto
                   {
                       AttackType = g.Key,
                       Count = g.Count(),
                       Percentage = Math.Round((double)g.Count() / totalIncidents * 100, 2)
                   }).OrderByDescending(dto => dto.Count).ToListAsync();
            }

            var summary = new DashboardSummaryDto
            {
                TotalIncidents = totalIncidents,
                IncidentsLast24Hours = incidentsLast24Hours,
                TopDangerousIps = topDangerousIps,
                AttackTypeDistribution = attackTypeDistribution
            };
            return Ok(summary);
        }

        // =================================================================
        // == 2. قسم عرض تفاصيل الحوادث (من IncidentsController)
        // =================================================================
        [HttpGet("incidents")] // GET /api/dashboard/incidents
        public async Task<IActionResult> GetIncidents([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var incidents = await _context.Incidents
                .OrderByDescending(i => i.IncidentTime)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .Select(i => new IncidentSummaryDto
                {
                    Id = i.Id,
                    AttackerIp = i.AttackerIp,
                    AttackTypeGuess = i.AttackTypeGuess,
                    IncidentTime = i.IncidentTime,
                    Country = i.Country
                }).ToListAsync();

            var totalCount = await _context.Incidents.CountAsync();
            return Ok(new { data = incidents, totalCount });
        }

        [HttpGet("incidents/{id}")] // GET /api/dashboard/incidents/5
        public async Task<IActionResult> GetIncidentById(int id)
        {
            var incident = await _context.Incidents
                .Include(i => i.RequestLogs)
                .Select(i => new IncidentDetailDto
                {
                    Id = i.Id,
                    AttackerIp = i.AttackerIp,
                    AttackTypeGuess = i.AttackTypeGuess,
                    IncidentTime = i.IncidentTime,
                    Country = i.Country,
                    City = i.City,
                    UserAgent = i.UserAgent,
                    Latitude = i.Latitude,
                    Longitude = i.Longitude,
                    RequestLogs = i.RequestLogs.Select(log => new RequestLogDto
                    {
                        Method = log.Method,
                        FullUrl = log.FullUrl,
                        Headers = log.Headers,
                        Body = log.Body
                    })
                }).FirstOrDefaultAsync(i => i.Id == id);

            if (incident == null) return NotFound();
            return Ok(incident);
        }

        // =================================================================
        // == 3. قسم الخريطة التفاعلية (من MapController)
        // =================================================================
        [HttpGet("maps/attack-origins")] // GET /api/dashboard/maps/attack-origins
        public async Task<IActionResult> GetAttackOrigins()
        {
            var attackOrigins = await _context.Incidents
                .Where(i => i.Latitude != null && i.Longitude != null)
                .Select(i => new AttackOriginDto
                {
                    Latitude = i.Latitude.Value,
                    Longitude = i.Longitude.Value,
                    AttackType = i.AttackTypeGuess,
                    City = i.City
                }).ToListAsync();
            return Ok(attackOrigins);
        }

        // =================================================================
        // == 4. قسم إدارة الأمان (من SecurityController)
        // =================================================================
        [HttpGet("security/profiles")] // GET /api/dashboard/security/profiles
        public async Task<IActionResult> GetSecurityProfiles([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var profiles = await _context.SecurityProfiles
                .OrderBy(p => p.ReputationScore)
                .Skip((pageNumber - 1) * pageSize).Take(pageSize)
                .Select(p => new SecurityProfileDto
                {
                    Id = p.Id,
                    IpAddress = p.IpAddress,
                    FingerprintHash = p.FingerprintHash,
                    ReputationScore = p.ReputationScore,
                    LastSeen = p.LastSeen
                }).ToListAsync();

            var totalCount = await _context.SecurityProfiles.CountAsync();
            return Ok(new { data = profiles, totalCount });
        }

        [HttpPost("security/block-ip")] // POST /api/dashboard/security/block-ip
        public async Task<IActionResult> BlockIp([FromBody] BlockIpRequest request)
        {
            var existingBlock = await _context.IpBlocks.FirstOrDefaultAsync(b => b.IpAddress == request.IpAddress);
            if (existingBlock != null) return BadRequest(new { message = "IP is already blocked." });

            var block = new IpBlock
            {
                IpAddress = request.IpAddress,
                Reason = request.Reason,
                ExpiresAt = DateTime.UtcNow.AddHours(request.DurationInHours)
            };
            _context.IpBlocks.Add(block);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"IP {request.IpAddress} has been blocked successfully." });
        }

        [HttpDelete("security/unblock-ip/{ipAddress}")] // DELETE /api/dashboard/security/unblock-ip/1.1.1.1
        public async Task<IActionResult> UnblockIp(string ipAddress)
        {
            var ipToUnblock = ipAddress.Replace("%2F", "/");
            var block = await _context.IpBlocks.FirstOrDefaultAsync(b => b.IpAddress == ipToUnblock);

            if (block == null) return NotFound(new { message = "IP is not on the blocklist." });

            _context.IpBlocks.Remove(block);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"IP {ipToUnblock} has been unblocked." });
        }
    }
}