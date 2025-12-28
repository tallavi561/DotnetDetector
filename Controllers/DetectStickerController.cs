using Microsoft.AspNetCore.Mvc;

namespace StickersDetector.Controllers
{
    [ApiController]
    [Route("api/detectSticker/v1")]
    public class DetectStickerController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> DetectSticker([FromForm] IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest("Image file is missing");
            }

            // Example: read image bytes (later you’ll pass this to OpenCV / ONNX)
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            byte[] imageBytes = memoryStream.ToArray();

            // For now – just return Hello World
            return Ok(new
            {
                message = "HELLO WORLD",
                fileName = image.FileName,
                fileSize = image.Length
            });
        }
    }
}
