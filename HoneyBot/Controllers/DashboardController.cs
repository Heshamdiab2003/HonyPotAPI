
using global::HoneyBot.Dros;
using global::HoneyBot.Dtos;
using HoneyBot.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

    namespace HoneyBot.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- نقطة النهاية الرئيسية: GET /api/dashboard/summary ---
        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            // 1. حساب إجمالي الحوادث وعددها في آخر 24 ساعة
            var totalIncidents = await _context.Incidents.LongCountAsync();
            var incidentsLast24Hours = await _context.Incidents
                .CountAsync(i => i.IncidentTime >= DateTime.UtcNow.AddHours(-24));

            // 2. جلب أخطر 5 عناوين IP (الأقل في نقاط السمعة)
            var topDangerousIps = await _context.SecurityProfiles
                .OrderBy(p => p.ReputationScore) // رتب تصاعديًا حسب النقاط
                .Take(5) // اختر أول 5 فقط
                .Select(p => new DangerousIpDto // حوّل البيانات إلى DTO
                {
                    IpAddress = p.IpAddress,
                    ReputationScore = p.ReputationScore,
                    LastSeen = p.LastSeen
                })
                .ToListAsync();

            // 3. حساب توزيع أنواع الهجمات
            var attackTypeDistribution = await _context.Incidents
                .GroupBy(i => i.AttackTypeGuess) // جمّع حسب نوع الهجوم
                .Select(g => new AttackTypeDistributionDto
                {
                    AttackType = g.Key, // اسم الهجوم
                    Count = g.Count(), // عدد مرات حدوثه
                                       // حساب النسبة المئوية
                    Percentage = Math.Round((double)g.Count() / totalIncidents * 100, 2)
                })
                .OrderByDescending(dto => dto.Count) // رتب من الأكثر شيوعًا إلى الأقل
                .ToListAsync();

            // 4. تجميع كل الإحصائيات في كائن واحد وإرسالها
            var summary = new DashboardSummaryDto
            {
                TotalIncidents = totalIncidents,
                IncidentsLast24Hours = incidentsLast24Hours,
                TopDangerousIps = topDangerousIps,
                AttackTypeDistribution = attackTypeDistribution
            };

            return Ok(summary);
        }
    }

