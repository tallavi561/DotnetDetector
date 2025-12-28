using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
namespace FindLabel;
public class StickerDetectionApp
{
    public static string Main(string uploadsPath, string outputPath, string patternFileName, Form1 frm)
    {
        // הגדרת קידוד UTF-8 לתמיכה בעברית בקונסול
        //Console.OutputEncoding = Encoding.UTF8;
        string outputFilePath = "";
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          מערכת זיהוי מדבקות מתקדמת - .NET Edition           ║");
        Console.WriteLine("║                  Advanced Sticker Detector                    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            // ניתוח ארגומנטים של שורת הפקודה
            string location = Path.GetDirectoryName(uploadsPath);
            //string outputPath = location +  "\\outputs";
            //string patternFileName =  "patternDoar.jpg";

            string patternPath = Path.Combine(location, patternFileName + ".jpg");
            //patternPath = location + "\\" + patternPath ;
            // בדיקת קיום הנתיבים
            if (!Directory.Exists(location))
            {
                Console.WriteLine($"❌ תיקיית הקלט לא קיימת: {location}");
                throw new Exception($"❌ תיקיית הקלט לא קיימת: {location}");
            }

            if (!File.Exists(patternPath))
            {
                Console.WriteLine($"❌ קובץ התבנית לא נמצא: {patternPath}");
                throw new Exception($"❌ קובץ התבנית לא נמצא: {patternPath}");
            }

            // יצירת תיקיית פלט
            Directory.CreateDirectory(outputPath);

            Console.WriteLine("📁 נתיבים:");
            Console.WriteLine($"   קלט:   {uploadsPath}");
            Console.WriteLine($"   פלט:   {outputPath}");
            Console.WriteLine($"   תבנית: {patternPath}");
            Console.WriteLine();

            // אתחול הדטקטור
            Console.WriteLine("🔧 מאתחל דטקטור...");
            if (!File.Exists(patternPath))
                throw new FileNotFoundException($"קובץ התבנית לא נמצא: {patternPath}");
            using var template = CvInvoke.Imread(patternPath, ImreadModes.Grayscale);
            

            // קריאת תמונת התבנית
            
            if (template.IsEmpty)
                throw new InvalidOperationException($"לא ניתן לקרוא את תמונת התבנית: {patternPath}");


            using var detector = new AdvancedStickerDetector(patternPath, template);
            Console.WriteLine("✓ הדטקטור מוכן");
            Console.WriteLine();

            // קבלת כל תמונות החבילות
            /* var packageImages = Directory.GetFiles(uploadsPath, "*.jpg")
                 .Where(f => !f.Contains(patternFileName))
                 .OrderBy(f => f)
                 .ToList();

             Console.WriteLine($"📸 נמצאו {packageImages.Count} תמונות לעיבוד");
             Console.WriteLine();
             Console.WriteLine(new string('═', 70));*/

            // סטטיסטיקות
            int totalDetections = 0;
            //int totalImages = packageImages.Count;
            int imagesWithDetections = 0;
            var detectionTimes = new List<TimeSpan>();

            // עיבוד כל תמונה
            /*for (int idx = 0; idx < packageImages.Count; idx++)
            {*/
            var imagePath = uploadsPath; //packageImages[idx];
            string fileName = Path.GetFileName(imagePath);

            /* Console.WriteLine();
             Console.WriteLine($"[{idx + 1}/{packageImages.Count}] {fileName}");
            */
            
            var startTime = DateTime.Now;

            try
            {
                // קריאת התמונה
                using var image = CvInvoke.Imread(imagePath, ImreadModes.Grayscale);
                
                if (image.IsEmpty)
                {
                    Console.WriteLine("  ❌ שגיאה בקריאת התמונה");
                    throw new Exception("  ❌ שגיאה בקריאת התמונה");
                }

                //Console.WriteLine($"  📐 גודל תמונה: {image.Width}x{image.Height}");
                var _size = frm.Controls
                  .Find("txtSize", true)
                  .OfType<TextBox>()
                  .FirstOrDefault();

                if (_size == null)
                    _size.Text = $" גודל תמונה: {image.Width}x{image.Height}";
                // חיפוש מדבקות
                var detections = detector.FindStickers(
                        image,
                        minMatches: 15,
                        ratioThreshold: 0.7,
                        ransacThreshold: 5.0);

                var processingTime = DateTime.Now - startTime;
                var _processTime = frm.Controls
                  .Find("txtProcessTime", true)
                  .OfType<TextBox>()
                  .FirstOrDefault();
                _processTime.Text = $"  זמן עיבוד: {processingTime.TotalSeconds:F2} שניות";
                detectionTimes.Add(processingTime);

                if (detections.Count > 0)
                {
                    Console.WriteLine($"  ✓ נמצאו {detections.Count} מדבקות!");
                    totalDetections += detections.Count;
                    imagesWithDetections++;

                    // הצגת פרטי כל זיהוי
                    for (int i = 0; i < detections.Count; i++)
                    {
                        var det = detections[i];
                        Console.WriteLine($"    מדבקה {i + 1}:");
                        Console.WriteLine($"      • רמת ביטחון: {det.Confidence:P0}");
                        Console.WriteLine($"      • התאמות: {det.Inliers} inliers מתוך {det.TotalMatches}");
                        Console.WriteLine($"      • מרכז: ({det.Center.X:F0}, {det.Center.Y:F0})");
                        Console.WriteLine($"      • שטח: {det.Area:F0} פיקסלים");
                    }
                }
                else
                {
                    Console.WriteLine("  ⚠ לא נמצאו מדבקות");
                }

                // סימון המדבקות בתמונה
                using var result = detector.DrawDetections(image, detections);

                // שמירת התוצאה
                string outputFileName = $"detected_{fileName}";
                outputFilePath = Path.Combine(outputPath, outputFileName);
                CvInvoke.Imwrite(outputFilePath, result);

                Console.WriteLine($"  💾 נשמר: {outputFileName}");
                Console.WriteLine($"  ⏱ זמן עיבוד: {processingTime.TotalSeconds:F2} שניות");

                RotatedLabelDetector.DetectAndCropRotatedLabels(outputPath, Path.GetFileName(uploadsPath), image, detections);
            }
            catch (Exception ex)
            {
               throw new Exception ($"  ❌ שגיאה: {ex.Message}");
            }


            // סיכום
            Console.WriteLine();
            Console.WriteLine(new string('═', 70));
            Console.WriteLine();
            Console.WriteLine("📊 סיכום:");
            // Console.WriteLine($"   • סה\"כ תמונות עובדו: {totalImages}");
            // Console.WriteLine($"   • תמונות עם זיהויים: {imagesWithDetections} ({100.0 * imagesWithDetections / totalImages:F1}%)");
            Console.WriteLine($"   • סה\"כ מדבקות שזוהו: {totalDetections}");

            if (detectionTimes.Count > 0)
            {
                var avgTime = detectionTimes.Average(t => t.TotalSeconds);
                var minTime = detectionTimes.Min(t => t.TotalSeconds);
                var maxTime = detectionTimes.Max(t => t.TotalSeconds);

                Console.WriteLine();
                Console.WriteLine("⏱ ביצועים:");
                Console.WriteLine($"   • זמן ממוצע לתמונה: {avgTime:F2} שניות");
                Console.WriteLine($"   • זמן מינימלי: {minTime:F2} שניות");
                Console.WriteLine($"   • זמן מקסימלי: {maxTime:F2} שניות");
            }

            Console.WriteLine();
            Console.WriteLine($"✓ הסתיים בהצלחה! כל התוצאות נשמרו ב: {outputPath}");

            return outputFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("❌ שגיאה קריטית:");
            Console.WriteLine($"   {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Stack trace:");
            Console.WriteLine(ex.StackTrace);

            return "";
        }
    }
}