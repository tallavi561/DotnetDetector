using Microsoft.AspNetCore.Mvc;
using Emgu.CV;
using Emgu.CV.CvEnum;
using StickersDetector.bl.OpenCV;
using StickersDetector.Models.Shapes;

namespace StickersDetector.Controllers
{
    [ApiController]
    [Route("api/detectSticker/v1")]
    public class DetectStickerController : ControllerBase
    {
        private readonly LabelDetector _labelDetector;

        public DetectStickerController(LabelDetector labelDetector)
        {
            _labelDetector = labelDetector;
        }

        [HttpPost]
        public async Task<IActionResult> DetectSticker([FromForm] IFormFile image, [FromForm] string labelName)
        {
            Console.WriteLine("Got new Message");
            // 1. Basic validation
            if (image == null || image.Length == 0) return BadRequest("Image file is missing");
            if (string.IsNullOrEmpty(labelName)) return BadRequest("labelName is required");
            Console.WriteLine("Got new Message");

            try
            {
                // 2. Read the file into a byte array and convert to an OpenCV Mat
                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);
                byte[] imageBytes = memoryStream.ToArray();

                using var inputImage = new Mat();
                CvInvoke.Imdecode(imageBytes, ImreadModes.Color, inputImage);

                if (inputImage.IsEmpty) return BadRequest("Invalid image format");


                // 3. Perform sticker detection
                Console.WriteLine($"[DEBUG] Attempting to detect label: '{labelName}'...");

                var detection = _labelDetector.Detect(inputImage, labelName);

                if (detection == null)
                {
                    return NotFound(new { message = $"Label '{labelName}' not detected or confidence too low. detection: {detection}" });
                }

                // 4. Crop and align the label using the detected corners
                using var alignedLabel = ImageRotator.ExtractAndAlignLabel(inputImage, detection.Corners);

                // 5. Convert the result back to bytes to return the image via API                using var outputStream = new MemoryStream();
                byte[] resultBytes = CvInvoke.Imencode(".jpg", alignedLabel);

                // Return the image file directly to the client
                return File(resultBytes, "image/jpeg", $"detected_{labelName}.jpg");
            }
            catch (KeyNotFoundException)
            {
                return BadRequest($"Label type '{labelName}' is not defined in the system.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }
    }
}