using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace HoneyBot.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileUploadController : ControllerBase
    {
        [HttpPost("upload-profile-picture")]
        public IActionResult UploadProfilePicture(IFormFile file)
        {
            // هذا مجرد فخ. نحن لا نحفظ الملف أبدًا.
            // نرجع ردًا وهميًا ومقنعًا.
            return Ok(new
            {
                message = "File uploaded successfully and is pending review.",
                fileName = file?.FileName ?? "unknown",
                fileId = Guid.NewGuid().ToString()
            });
        }
    }
}