using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HoneyBot.Controllers
{
    [ApiController]
    [Route("api")]
    public class SearchController : ControllerBase
    {
        [HttpGet("search")]
        public IActionResult Search([FromQuery] string q)
        {
            // الخدعة: نرجع دائمًا نتيجة فارغة، بغض النظر عن مدى خطورة الإدخال
            // الكشف الحقيقي تم بالفعل في الـ Middleware
            Console.WriteLine($"[FAKE SEARCH] Received search query: {q}");
            return Ok(new { results_count = 0, items = new object[] { } });
        }
    }
}
