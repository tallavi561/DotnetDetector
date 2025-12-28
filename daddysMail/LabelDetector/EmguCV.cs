using System;
using System.IO;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace FindLabel
{
    public  class EmguCV
    {
          /*  public static string Main(string uploadsPath)
            {
                try
                {
                    Console.OutputEncoding = System.Text.Encoding.UTF8;

                    // נתיבים
                    string locat = args.Length > 0 ? args[0] : @"uploads";
                    string outputPath = args.Length > 1 ? args[1] : @"outputs";
                    string patternPath = Path.Combine(uploadsPath, "pattern.jpg");

                    // יצירת תיקיית פלט
                    Directory.CreateDirectory(outputPath);

                    Console.WriteLine("=== זיהוי מדבקות מתקדם ===");
                    Console.WriteLine("מאתחל דטקטור...\n");

                    // אתחול הדטקטור
                    using (var detector = new AdvancedStickerDetector(patternPath))
                    {
                        // קבלת כל תמונות החבילות
                        var packageImages = Directory.GetFiles(uploadsPath, "*.jpg")
                            .Where(f => !f.Contains("pattern.jpg"))
                            .OrderBy(f => f)
                            .ToList();

                        Console.WriteLine($"נמצאו {packageImages.Count} תמונות לעיבוד\n");
                        Console.WriteLine(new string('=', 70));

                        int totalDetections = 0;

                        // עיבוד כל תמונה
                        foreach (var imagePath in packageImages)
                        {
                            string fileName = Path.GetFileName(imagePath);
                            Console.WriteLine($"\nמעבד: {fileName}");

                            // קריאת התמונה
                            using (var image = CvInvoke.Imread(imagePath, ImreadModes.Color))
                            {
                                if (image.IsEmpty)
                                {
                                    Console.WriteLine("  ❌ שגיאה בקריאת התמונה");
                                    continue;
                                }

                                Console.WriteLine($"  גודל תמונה: {image.Width}x{image.Height}");

                                // חיפוש מדבקות
                                var detections = detector.FindStickers(image, minMatches: 15);

                                if (detections.Count > 0)
                                {
                                    Console.WriteLine($"  ✓ נמצאו {detections.Count} מדבקות!");
                                    for (int i = 0; i < detections.Count; i++)
                                    {
                                        var det = detections[i];
                                        Console.WriteLine($"    מדבקה {i + 1}: רמת ביטחון {det.Confidence:P0}, " +
                                                        $"{det.Inliers} inliers מתוך {det.Matches} התאמות");
                                    }
                                    totalDetections += detections.Count;
                                }
                                else
                                {
                                    Console.WriteLine("  ⚠ לא נמצאו מדבקות");
                                }

                                // סימון המדבקות
                                using (var result = detector.DrawDetections(image, detections))
                                {
                                    // שמירת התוצאה
                                    string outputFileName = $"detected_{fileName}";
                                    string outputFilePath = Path.Combine(outputPath, outputFileName);
                                    CvInvoke.Imwrite(outputFilePath, result);
                                    Console.WriteLine($"  💾 נשמר: {outputFileName}");
                                }
                            }
                        }

                        Console.WriteLine("\n" + new string('=', 70));
                        Console.WriteLine($"הסתיים בהצלחה! סה\"כ נמצאו {totalDetections} מדבקות");
                        Console.WriteLine($"כל התמונות נשמרו בתיקייה: {outputPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ שגיאה: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
        }
    }*/

  }
}

