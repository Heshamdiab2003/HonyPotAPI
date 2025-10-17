using Microsoft.AspNetCore.Mvc;

namespace HoneyBot.Controllers
{
    [ApiController]
    [Route("api")]
    public class SearchController : ControllerBase
    {
        // The [HttpGet("search")] attribute tells this method to listen for GET requests to /api/search
        [HttpGet("search")]
        // The [FromQuery] attribute is crucial. It tells the method to find a parameter named "q"
        // in the URL's query string (e.g., /api/search?q=somevalue)
        public IActionResult Search([FromQuery] string q)
        {
            // This is just a decoy. The real analysis has already happened in the Middleware.
            // We always return an empty result to deceive the attacker.
            return Ok(new { results_count = 0, items = new object[] { } });
        }
    }
}