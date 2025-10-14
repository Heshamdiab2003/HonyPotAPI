// --- 1. كل الـ using statements المطلوبة ---
using HoneyBot;
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

        public HoneypotMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // --- 3. الدالة الرئيسية التي تعالج كل طلب ---
        public async Task InvokeAsync(HttpContext context, ApplicationDbContext db, IAttackAnalysisService analysisService, IGeolocationService geoService, IMemoryCache cache)
        {
            var attackerIp = context.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(attackerIp))
            {
                await _next(context);
                return;
            }

            // --- الخطوة أ: جلب أو إنشاء الملف الأمني للزائر ---
            var securityProfile = await db.SecurityProfiles.FirstOrDefaultAsync(p => p.IpAddress == attackerIp);
            if (securityProfile == null)
            {
                securityProfile = new SecurityProfile { IpAddress = attackerIp };
                db.SecurityProfiles.Add(securityProfile);
                // سيتم الحفظ لاحقًا لتجنب استعلامين لقاعدة البيانات
            }

            // --- الخطوة ب (المنطق الجديد والحاسم): التحقق من السمعة أولاً ---
            // إذا كان رصيد السمعة أقل من 20، قم بالحظر فورًا وأوقف كل شيء.
            if (securityProfile.ReputationScore < 20)
            {
                Console.WriteLine($"[BLOCK] IP {attackerIp} blocked. Reputation score is {securityProfile.ReputationScore} (below threshold of 20).");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access Denied due to suspicious activity.");
                return; // <-- أهم سطر: أوقف الطلب هنا
            }

            // --- الخطوة ج: تحليل الطلب الحالي وخصم النقاط (إذا لم يتم حظره) ---
            var allInputs = new StringBuilder();
            allInputs.Append(context.Request.Path.ToString().ToLower());
            allInputs.Append(context.Request.QueryString.ToString().ToLower());

            context.Request.EnableBuffering();
            var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            allInputs.Append(requestBody.ToLower());

            var attackType = analysisService.AnalyzeRequest(allInputs.ToString(), context.Request.Headers["User-Agent"].ToString());

            if (attackType != "Normal")
            {
                await LogAttackAndDeductPointsAsync(db, securityProfile, attackType, context, requestBody, geoService);
            }

            // تحديث آخر وقت تمت فيه رؤية الزائر
            securityProfile.LastSeen = DateTime.UtcNow;
            await db.SaveChangesAsync(); // احفظ كل التغييرات في قاعدة البيانات مرة واحدة

            // تمرير الطلب إلى الـ Controller إذا كان كل شيء على ما يرام
            await _next(context);
        }

        // --- 4. دالة مساعدة لتسجيل الهجوم وخصم النقاط ---
        private async Task LogAttackAndDeductPointsAsync(ApplicationDbContext db, SecurityProfile profile, string attackType, HttpContext context, string body, IGeolocationService geoService)
        {
            var honeypot = await db.Honeypots.FirstOrDefaultAsync();
            if (honeypot == null) return;

            // خصم النقاط بناءً على نوع الهجوم
            int pointsToDeduct = attackType switch
            {
                "SQL Injection" => 50,
                "Directory Traversal / LFI" => 60,
                _ => 30 // قيمة افتراضية للهجمات الأخرى
            };
            profile.ReputationScore -= pointsToDeduct;
            profile.Notes = $"Last detected issue: {attackType} at {DateTime.UtcNow}. Score deducted: {pointsToDeduct}.";

            Console.WriteLine($"[SECURITY EVENT] IP: {profile.IpAddress}, Event: {attackType}, Score deducted: {pointsToDeduct}, New Score: {profile.ReputationScore}");


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
            // لا نحتاج لـ SaveChangesAsync هنا، لأنها ستُستدعى في InvokeAsync
        }
    }

    // --- 5. الـ Extension Method لتسجيل الـ Middleware في Program.cs ---
    public static class HoneypotMiddlewareExtensions
    {
        public static IApplicationBuilder UseHonyBotMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HoneypotMiddleware>();
        }
    }
}