// --- 1. كل الـ using statements المطلوبة ---
using HoneyBot;
using HoneyBot.Models;
using HoneyBot.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Net;

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
            // الخطوة 0: تجاهل مسارات Swagger والملفات الثابتة لتجنب المشاكل
            if (context.Request.Path.StartsWithSegments("/swagger") || 
                context.Request.Path.StartsWithSegments("/honeypot-tester.html") ||
                context.Request.Path.Value.EndsWith(".html") ||
                context.Request.Path.Value.EndsWith(".css") ||
                context.Request.Path.Value.EndsWith(".js") ||
                context.Request.Path.Value.EndsWith(".png") ||
                context.Request.Path.Value.EndsWith(".jpg") ||
                context.Request.Path.Value.EndsWith(".ico"))
            {
                await _next(context);
                return;
            }

            var attackerIp = context.Connection.RemoteIpAddress?.ToString();
            string? visitorId = context.Request.Headers["X-Visitor-ID"].FirstOrDefault();

            if (string.IsNullOrEmpty(attackerIp))
            {
                await _next(context);
                return;
            }

            // الخطوة أ: البحث الشامل عن الملف الأمني
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

            // الخطوة ب: التحقق من السمعة وتنفيذ الحظر
            if (securityProfile.ReputationScore < 20)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await db.SaveChangesAsync();
                return;
            }

            // الخطوة ج: تحليل الطلب وخصم النقاط
			var allInputs = new StringBuilder();
			allInputs.Append(context.Request.Path.ToString().ToLower());
			// Decode URL-encoded query string to ensure patterns like %3Cscript%3E are detected
			var rawQuery = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
			var decodedQuery = string.IsNullOrEmpty(rawQuery) ? string.Empty : WebUtility.UrlDecode(rawQuery);
			allInputs.Append((decodedQuery ?? string.Empty).ToLower());
            context.Request.EnableBuffering();
            var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
			var requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
			// Decode URL-encoded bodies (e.g., application/x-www-form-urlencoded)
			var decodedBody = string.IsNullOrEmpty(requestBody) ? string.Empty : WebUtility.UrlDecode(requestBody);
			allInputs.Append((decodedBody ?? string.Empty).ToLower());

			if (context.Request.HasFormContentType)
            {
				// Include form fields (decoded) for signature analysis
				foreach (var key in context.Request.Form.Keys)
				{
					var value = context.Request.Form[key];
					allInputs.Append(WebUtility.UrlDecode(key).ToLower());
					allInputs.Append(WebUtility.UrlDecode(value).ToLower());
				}
				// Include uploaded filenames
				if (context.Request.Form.Files.Any())
				{
					foreach (var file in context.Request.Form.Files)
					{
						allInputs.Append(file.FileName.ToLower());
					}
				}
            }

            var attackType = analysisService.AnalyzeRequest(allInputs.ToString(), context.Request.Headers["User-Agent"].ToString());
            Console.WriteLine($"[MIDDLEWARE DEBUG] Attack type detected: {attackType}");

            if (attackType != "Normal")
            {
                Console.WriteLine($"[MIDDLEWARE DEBUG] Logging attack: {attackType}, Current score: {securityProfile.ReputationScore}");
                await LogAttackAndDeductPointsAsync(db, securityProfile, attackType, context, requestBody, geoService);
                Console.WriteLine($"[MIDDLEWARE DEBUG] After deduction, new score: {securityProfile.ReputationScore}");
                // If reputation dropped below threshold due to this attack, block immediately
                if (securityProfile.ReputationScore < 20)
                {
                    securityProfile.LastSeen = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
            }

            securityProfile.LastSeen = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await _next(context);
        }

        // --- 4. دالة مساعدة لتسجيل الهجوم وخصم النقاط (النسخة الكاملة والمصححة) ---
        private async Task LogAttackAndDeductPointsAsync(ApplicationDbContext db, SecurityProfile profile, string attackType, HttpContext context, string body, IGeolocationService geoService)
        {
            Console.WriteLine($"[LOGGING DEBUG] Starting to log attack: {attackType} for profile ID: {profile.Id}");
            
            var honeypot = await db.Honeypots.FirstOrDefaultAsync();
            if (honeypot == null)
            {
                Console.WriteLine("[LOGGING FAILED] No honeypots found in DB. Incident was NOT saved.");
                return;
            }

            // **الإصلاح الأهم:** تحديد النقاط التي سيتم خصمها ديناميكيًا
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

            var requestLog = new RequestLog
            {
                Incident = incident,
                Method = context.Request.Method,
                FullUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}",
                Body = body,
                Headers = string.Join("\n", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"))
            };

            db.Incidents.Add(incident);
            db.RequestLogs.Add(requestLog); // التأكد من إضافة تفاصيل الطلب
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