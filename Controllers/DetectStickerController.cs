using Microsoft.AspNetCore.Mvc;
using StickersDetector.bl.OpenCV;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // עבור IFormFile
using System.IO;                // עבור MemoryStream
using System.Collections.Generic; // עבור List
using System.Threading.Tasks;    // עבור Task
using System;                   // עבור Exception ו-Convert

// Emgu.CV Usings
using Emgu.CV;
using Emgu.CV.CvEnum;
using StickersDetector.bl.OpenCV; // ה-Namespace של הלוגיקה שלך

namespace StickersDetector.Controllers
{
    public class DetectionResponse
    {
        public string LabelName { get; set; }
        public double Confidence { get; set; }
        public string ImageBase64 { get; set; } // התמונה הגזורה בלבד
    }

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
            // calculate start time
            var startTime = DateTime.Now;
            Console.WriteLine($"[INFO] Received detection request for label: {labelName}");
            if (image == null || image.Length == 0) return BadRequest("Image file is missing");
            if (string.IsNullOrEmpty(labelName)) return BadRequest("labelName is required");
            Console.WriteLine($"[INFO] Image size: {image.Length} bytes");
            try
            {
                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);
                
                using var inputImage = new Mat();
                // שימוש ב-Unchanged עבור תמונות MONO/Grayscale
                CvInvoke.Imdecode(memoryStream.ToArray(), ImreadModes.Unchanged, inputImage);

                if (inputImage.IsEmpty) return BadRequest("Invalid image format");

                var detections = _labelDetector.Detect(inputImage, labelName);
                // calculate time of detection
                var detectionTime = DateTime.Now;
                Console.WriteLine($"[INFO] Detection time: {(detectionTime - startTime).TotalMilliseconds} ms");
                if (detections == null || detections.Count == 0)
                {
                    return Ok(new List<DetectionResponse>());
                }

                var responseList = new List<DetectionResponse>();

                foreach (var detection in detections)
                {
                    try
                    {
                        // חיתוך ויישור
                        using var alignedLabel = ImageRotator.ExtractAndAlignLabel(inputImage, detection.Corners);
                        
                        // קידוד ל-JPG (שומר על ערוץ אחד ב-MONO)
                        byte[] croppedBytes = CvInvoke.Imencode(".jpg", alignedLabel);

                        responseList.Add(new DetectionResponse
                        {
                            LabelName = detection.LabelName,
                            Confidence = detection.Confidence,
                            ImageBase64 = Convert.ToBase64String(croppedBytes)
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WARNING] Failed to crop detection: {ex.Message}");
                    }
                }
                Console.WriteLine($"[INFO] Detection completed. Found {responseList.Count} instances of label: {labelName}");
                var endTime = DateTime.Now;
                Console.WriteLine($"[INFO] Processing time: {(endTime - startTime).TotalMilliseconds} ms");
                return Ok(responseList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }
    }
}