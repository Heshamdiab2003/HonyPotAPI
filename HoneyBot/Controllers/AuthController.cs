using HoneyBot;
using HoneyBot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoneyBot.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    // We inject the DbContext to interact with the database
    public AuthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        Console.WriteLine("\n--- New Login Attempt Received ---");

        // --- 1. التحقق من صحة بيانات الدخول (المنطق الحقيقي) ---
        var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == loginRequest.Email);

        if (student != null && student.PasswordHash == loginRequest.Password)
        {
            // --- تسجيل الدخول ناجح ---
            Console.WriteLine($"Login SUCCEEDED for user: {loginRequest.Email}");
            return Ok(new { message = $"Welcome, {student.Email}!" });
        }

        // --- تسجيل الدخول فاشل ---
        Console.WriteLine($"Login FAILED for user: {loginRequest.Email}. Starting reputation deduction logic.");

        // --- 2. منطق خصم النقاط (الجزء الأهم) ---
        var attackerIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(attackerIp))
        {
            Console.WriteLine("Could not get attacker IP. Aborting score deduction.");
            return Unauthorized(new { message = "Invalid email or password." });
        }

        Console.WriteLine($"Attacker IP: {attackerIp}");

        // ابحث عن الملف الأمني للـ IP
        var securityProfile = await _context.SecurityProfiles.FirstOrDefaultAsync(p => p.IpAddress == attackerIp);

        if (securityProfile != null)
        {
            Console.WriteLine($"Security Profile Found! Current Score: {securityProfile.ReputationScore}");

            // اخصم 10 نقاط مع كل محاولة فاشلة
            securityProfile.ReputationScore -= 10;
            securityProfile.LastSeen = DateTime.UtcNow;
            securityProfile.Notes = $"Failed login attempt at {DateTime.UtcNow}.";

            // احفظ التغييرات في قاعدة البيانات
            await _context.SaveChangesAsync();

            Console.WriteLine($"SUCCESS: Score deducted. New Score: {securityProfile.ReputationScore}");
        }
        else
        {
            Console.WriteLine("WARNING: Security Profile NOT found for this IP. No score was deducted.");
            // ملاحظة: الـ Middleware يجب أن يكون قد أنشأ الملف بالفعل. إذا رأيت هذه الرسالة،
            // فهذا يعني أن الطلب لم يمر عبر الـ Middleware بشكل صحيح.
        }

        // أرجع دائمًا نفس رسالة الخطأ للمهاجم
        return Unauthorized(new { message = "Invalid email or password." });
    }
}

// نموذج بسيط لاستقبال بيانات الدخول (يمكن وضعه في نفس الملف أو في ملف منفصل)
public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}