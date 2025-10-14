using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HoneyBot.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileViewerController : ControllerBase
    {
        [HttpGet("view")]
        public IActionResult ViewFile([FromQuery] string path)
        {
            // الخدعة: نرجع دائمًا "ملف غير موجود"
            // الكشف الحقيقي عن "../" تم في الـ Middleware
            Console.WriteLine($"[FAKE VIEW] Received request to view file: {path}");
            return NotFound(new { error = "File does not exist." });
        }
    }
}
