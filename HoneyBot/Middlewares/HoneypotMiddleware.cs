//// --- 1. كل الـ using statements المطلوبة ---
//using HoneyBot;
//using HoneyBot.Models;
//using HoneyBot.Service;
//using Microsoft.AspNetCore.Http;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Memory;
//using System.Text;

//namespace HoneyBot.Middlewares
//{
//    // --- 2. تعريف الـ Middleware Class ---
//    public class HoneypotMiddleware
//    {
//        private readonly RequestDelegate _next;

//        public HoneypotMiddleware(RequestDelegate next) => _next = next;

//        // --- 3. الدالة الرئيسية التي تعالج كل طلب ---
//        public async Task InvokeAsync(HttpContext context, ApplicationDbContext db, IAttackAnalysisService analysisService, IGeolocationService geoService, IMemoryCache cache)
//        {
//            var attackerIp = context.Connection.RemoteIpAddress?.ToString();
//            string? visitorId = context.Request.Headers["X-Visitor-ID"].FirstOrDefault();

//            if (string.IsNullOrEmpty(attackerIp))
//            {
//                await _next(context);
//                return;
//            }

//            // --- الخطوة أ (المنطق الجديد والذكي): البحث الشامل عن الملف الأمني ---
//            SecurityProfile? securityProfile = null;

//            // 1. الأولوية للبحث بالبصمة
//            if (!string.IsNullOrEmpty(visitorId))
//            {
//                securityProfile = await db.SecurityProfiles.FirstOrDefaultAsync(p => p.FingerprintHash == visitorId);
//            }

//            // 2. إذا لم نجد بالبصمة، نبحث بالـ IP
//            if (securityProfile == null)
//            {
//                securityProfile = await db.SecurityProfiles.FirstOrDefaultAsync(p => p.IpAddress == attackerIp);
//            }

//            // 3. إنشاء أو تحديث الملف الأمني
//            if (securityProfile == null)
//            {
//                // إذا لم نجد أي شيء، ننشئ ملفًا جديدًا
//                securityProfile = new SecurityProfile { IpAddress = attackerIp, FingerprintHash = visitorId };
//                db.SecurityProfiles.Add(securityProfile);
//            }
//            else
//            {
//                // إذا وجدنا ملفًا، نقوم بتحديثه بالمعلومات الجديدة
//                securityProfile.IpAddress = attackerIp; // حدّث دائمًا آخر IP
//                if (string.IsNullOrEmpty(securityProfile.FingerprintHash) && !string.IsNullOrEmpty(visitorId))
//                {
//                    // هذه هي النقطة الأهم: ربط البصمة بالملف الموجود
//                    securityProfile.FingerprintHash = visitorId;
//                }
//            }

//            // --- الخطوة ب: التحقق من السمعة وتنفيذ الحظر ---
//            if (securityProfile.ReputationScore < 20)
//            {
//                // ... (منطق الحظر يظل كما هو)
//                context.Response.StatusCode = StatusCodes.Status403Forbidden;
//                await db.SaveChangesAsync();
//                return;
//            }

//            // --- الخطوة ج: تحليل الطلب وخصم النقاط ---
//            // ... (باقي الكود الخاص بتحليل الهجمات وخصم النقاط يظل كما هو) ...

//            securityProfile.LastSeen = DateTime.UtcNow;
//            await db.SaveChangesAsync();

//            await _next(context);
//        }


//        // --- 4. دالة مساعدة لتسجيل الهجوم وخصم النقاط ---
//        private async Task LogAttackAndDeductPointsAsync(ApplicationDbContext db, SecurityProfile profile, string attackType, HttpContext context, string body, IGeolocationService geoService)
//        {
//            var honeypot = await db.Honeypots.FirstOrDefaultAsync();
//            if (honeypot == null) return;

//            int pointsToDeduct = attackType switch
//            {
//                "SQL Injection" => 50,
//                "Directory Traversal / LFI" => 60,
//                _ => 30
//            };
//            profile.ReputationScore -= pointsToDeduct;
//            profile.Notes = $"Last issue: {attackType} at {DateTime.UtcNow}. Score deducted: {pointsToDeduct}.";

//            Console.WriteLine($"[SECURITY EVENT] Profile ID: {profile.Id}, Event: {attackType}, Score deducted: {pointsToDeduct}, New Score: {profile.ReputationScore}");

//            var geoInfo = await geoService.GetGeoInfoAsync(profile.IpAddress);
//            var incident = new Incident
//            {
//                AttackerIp = profile.IpAddress,
//                AttackTypeGuess = attackType,
//                UserAgent = context.Request.Headers["User-Agent"].ToString(),
//                Country = geoInfo?.Country ?? "Unknown",
//                City = geoInfo?.City ?? "Unknown",
//                Latitude = geoInfo?.Latitude,
//                Longitude = geoInfo?.Longitude,
//                HoneypotId = honeypot.Id
//            };

//            db.Incidents.Add(incident);
//        }
//    }

//    // --- 5. الـ Extension Method ---
//    public static class HoneypotMiddlewareExtensions
//    {
//        public static IApplicationBuilder UseHonyBotMiddleware(this IApplicationBuilder builder)
//        {
//            return builder.UseMiddleware<HoneypotMiddleware>();
//        }
//    }
//}


// --- 1. كل الـ using statements المطلوبة ---
using HoneyBot; // <--- تأكد من أن هذا الـ namespace صحيح لمشروعك
using HoneyBot.Models;
using HoneyBot.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace HoneyBot.Middlewares
{
    // --- 2. تعريف الـ Middleware Class ---
    public class HoneypotMiddleware
    {
        private readonly RequestDelegate _next;

        public HoneypotMiddleware(RequestDelegate next) => _next = next;

        // --- 3. الدالة الرئيسية التي تعالج كل طلب ---
        public async Task InvokeAsync(HttpContext context, ApplicationDbContext db, IAttackAnalysisService analysisService, IGeolocationService geoService, IMemoryCache cache)
        {
            var attackerIp = context.Connection.RemoteIpAddress?.ToString();
            string? visitorId = context.Request.Headers["X-Visitor-ID"].FirstOrDefault();

            if (string.IsNullOrEmpty(attackerIp))
            {
                await _next(context);
                return;
            }

            // --- الخطوة أ: البحث الشامل عن الملف الأمني ---
            SecurityProfile? securityProfile = null;

            if (!string.IsNullOrEmpty(visitorId))
            {
                securityProfile = await db.SecurityProfiles.FirstOrDefaultAsync(p => p.FingerprintHash == visitorId);
            }
            if (securityProfile == null)
            {
                securityProfile = await db.SecurityProfiles.FirstOrDefaultAsync(p => p.IpAddress == attackerIp);
            }
            if (securityProfile == null)
            {
                securityProfile = new SecurityProfile { IpAddress = attackerIp, FingerprintHash = visitorId };
                db.SecurityProfiles.Add(securityProfile);
            }
            else
            {
                securityProfile.IpAddress = attackerIp;
                if (string.IsNullOrEmpty(securityProfile.FingerprintHash) && !string.IsNullOrEmpty(visitorId))
                {
                    securityProfile.FingerprintHash = visitorId;
                }
            }

            // --- الخطوة ب: التحقق من السمعة وتنفيذ الحظر ---
            if (securityProfile.ReputationScore < 20)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await db.SaveChangesAsync(); // احفظ أي تحديثات قبل الحظر
                return;
            }

            // --- الخطوة ج (الجزء الذي تم إصلاحه وإضافته): تحليل الطلب وخصم النقاط ---
            var allInputs = new StringBuilder();
            allInputs.Append(context.Request.Path.ToString().ToLower());
            allInputs.Append(context.Request.QueryString.ToString().ToLower());

            context.Request.EnableBuffering();
            var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            allInputs.Append(requestBody.ToLower());

            if (context.Request.HasFormContentType && context.Request.Form.Files.Any())
            {
                foreach (var file in context.Request.Form.Files)
                {
                    allInputs.Append(file.FileName.ToLower());
                }
            }

            var attackType = analysisService.AnalyzeRequest(allInputs.ToString(), context.Request.Headers["User-Agent"].ToString());

            // إذا تم اكتشاف هجوم قائم على التوقيع، قم بتسجيله
            if (attackType != "Normal")
            {
                await LogAttackAndDeductPointsAsync(db, securityProfile, attackType, context, requestBody, geoService);
            }

            // منطق خاص بهجمات التخمين
            if (context.Request.Path.ToString().Contains("/login", StringComparison.OrdinalIgnoreCase) && context.Request.Method == "POST")
            {
                // هذا الكود يعمل بعد أن يرجع الـ Controller ردًا فاشلاً
                // سنحتاج لتعديل الـ AuthController ليخصم النقاط مباشرة ليكون أكثر دقة
            }

            securityProfile.LastSeen = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await _next(context);
        }

        // --- 4. دالة مساعدة لتسجيل الهجوم وخصم النقاط ---
        private async Task LogAttackAndDeductPointsAsync(ApplicationDbContext db, SecurityProfile profile, string attackType, HttpContext context, string body, IGeolocationService geoService)
        {
            var honeypot = await db.Honeypots.FirstOrDefaultAsync();
            if (honeypot == null) return;

            int pointsToDeduct = attackType switch
            {
                "SQL Injection" => 50,
                "Directory Traversal / LFI" => 60,
                "Cross-Site Scripting (XSS)" => 40,
                "Malicious File Upload" => 70,
                "Automated Tool Scan" => 25,
                _ => 30 // قيمة افتراضية لأي هجمات أخرى
            };
            profile.ReputationScore -= pointsToDeduct;
            profile.Notes = $"Last issue: {attackType} at {DateTime.UtcNow}. Score deducted: {pointsToDeduct}.";

            Console.WriteLine($"[SECURITY EVENT] Profile ID: {profile.Id}, Event: {attackType}, Score deducted: {pointsToDeduct}, New Score: {profile.ReputationScore}");

            var geoInfo = await geoService.GetGeoInfoAsync(profile.IpAddress);
            var incident = new Incident
            {
                AttackerIp = profile.IpAddress,
                AttackTypeGuess = attackType,
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                Country = geoInfo?.Country ?? "Unknown",
                City = geoInfo?.City ?? "Unknown",
                Latitude = geoInfo?.Latitude,
                Longitude = geoInfo?.Longitude,
                HoneypotId = honeypot.Id
            };

            db.Incidents.Add(incident);
        }
    }

    // --- 5. الـ Extension Method ---
    public static class HoneypotMiddlewareExtensions
    {
        public static IApplicationBuilder UseHonyBotMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HoneypotMiddleware>();
        }
    }
}