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

        // הזרקת ה-Detector שנרשם ב-Program.cs
        public DetectStickerController(LabelDetector labelDetector)
        {
            _labelDetector = labelDetector;
        }

        [HttpPost]
        public async Task<IActionResult> DetectSticker([FromForm] IFormFile image, [FromForm] string labelName)
        {
            // 1. וולידציה בסיסית
            if (image == null || image.Length == 0) return BadRequest("Image file is missing");
            if (string.IsNullOrEmpty(labelName)) return BadRequest("labelName is required");

            try
            {
                // 2. קריאת הקובץ למערך בייטים והמרה ל-Mat של OpenCV
                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);
                byte[] imageBytes = memoryStream.ToArray();

                using var inputImage = new Mat();
                CvInvoke.Imdecode(imageBytes, ImreadModes.Color, inputImage);

                if (inputImage.IsEmpty) return BadRequest("Invalid image format");

                // 3. ביצוע זיהוי המדבקה
                var detection = _labelDetector.Detect(inputImage, labelName);

                if (detection == null || !detection.IsReliable)
                {
                    return NotFound(new { message = $"Label '{labelName}' not detected or confidence too low." });
                }

                // 4. חיתוך ויישור המדבקה בעזרת הפינות שזוהו
                using var alignedLabel = ImageRotator.ExtractAndAlignLabel(inputImage, detection.Corners);

                // 5. המרת התוצאה חזרה לבייטים כדי להחזיר תמונה ב-API
                using var outputStream = new MemoryStream();
                byte[] resultBytes = CvInvoke.Imencode(".jpg", alignedLabel);

                // החזרת קובץ תמונה ישירות לדפדפן/לקוח
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