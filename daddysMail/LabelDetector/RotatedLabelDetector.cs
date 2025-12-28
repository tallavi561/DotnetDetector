using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Collections.Generic;


namespace FindLabel
{
        /// <summary>
        /// גלאי תוויות משופר עם תמיכה בתוויות מסובבות/אלכסוניות
        /// Enhanced label detector with support for rotated/diagonal labels
        /// </summary>
          
        public class RotatedLabelDetector
        {
            /// <summary>
            /// זהה וחתוך תוויות - תומך בתוויות מסובבות
            /// Detect and crop labels - supports rotated labels
            /// </summary>
            public static List<CroppedLabel> DetectAndCropRotatedLabels(
                                                    string outputFolder, 
                                                    string sourceName, 
                                                    Mat sourceImage,
                                                    List<StickerDetection> detections)
            {
                List<CroppedLabel> croppedLabels = new List<CroppedLabel>();

                Console.WriteLine("מזהה תוויות... / Detecting labels...");

                // שיטה 1: התאמת תכונות (תומך בסיבוב)
                // Method 1: Feature matching (supports rotation)
                

                if (detections.Count > 0)
                {
                    Console.WriteLine($"נמצאו {detections.Count} תוויות באמצעות התאמת תכונות");
                    Console.WriteLine($"Found {detections.Count} labels using feature matching");

                   // using (Mat sourceImage = CvInvoke.Imread(imagePath, ImreadModes.Grayscale))
                    {
                        for (int i = 0; i < detections.Count; i++)
                        {
                            var match = detections[i];

                            // חשב זווית סיבוב
                            // Calculate rotation angle
                            float angle = GlobalDetector.CalculateRotationAngle(match.Corners);

                            // חתוך עם תיקון פרספקטיבה (תומך בסיבוב)
                            // Crop with perspective correction (supports rotation)
                            Mat cropped = CropLabelWithPerspective(sourceImage, match.Corners);

                            string outputPath = null;
                            if (!string.IsNullOrEmpty(outputFolder))
                            {
                                if (!System.IO.Directory.Exists(outputFolder))
                                    System.IO.Directory.CreateDirectory(outputFolder);

                                outputPath = System.IO.Path.Combine(outputFolder, $"{sourceName}_label_{i + 1}_angle_{angle:F0}.jpg");
                                CvInvoke.Imwrite(outputPath, cropped);
                                Console.WriteLine($"  נשמר: {outputPath}");
                                Console.WriteLine($"  Saved: {outputPath}");
                            }

                            croppedLabels.Add(new CroppedLabel
                            {
                                Id = i + 1,
                                Image = cropped,
                                Corners = match.Corners,
                                RotationAngle = angle,
                                FilePath = outputPath,
                                DetectionMethod = "Feature Matching"
                            });
                        }
                    }
                }
                /*else
                {
                    // שיטה 2: התאמת תבנית (לתוויות ישרות)
                    // Method 2: Template matching (for straight labels)
                    Console.WriteLine("מנסה התאמת תבנית... / Trying template matching...");
                    var templateMatches = SimpleTemplateMatcher.FindLabelsByTemplate(
                        templatePath, imagePath, 0.6);

                    if (templateMatches.Count > 0)
                    {
                        Console.WriteLine($"נמצאו {templateMatches.Count} תוויות באמצעות התאמת תבנית");
                        Console.WriteLine($"Found {templateMatches.Count} labels using template matching");

                        using (Mat sourceImage = CvInvoke.Imread(imagePath, ImreadModes.Color))
                        {
                            for (int i = 0; i < templateMatches.Count; i++)
                            {
                                var match = templateMatches[i];
                                Rectangle rect = match.GetRectangle();

                                // חיתוך פשוט (תוויות ישרות)
                                // Simple crop (straight labels)
                                Mat roi = new Mat(sourceImage, rect);
                                Mat cropped = roi.Clone();

                                string outputPath = null;
                                if (!string.IsNullOrEmpty(outputFolder))
                                {
                                    if (!System.IO.Directory.Exists(outputFolder))
                                        System.IO.Directory.CreateDirectory(outputFolder);

                                    outputPath = System.IO.Path.Combine(outputFolder, $"label_{i + 1}.jpg");
                                    CvInvoke.Imwrite(outputPath, cropped);
                                    Console.WriteLine($"  נשמר: {outputPath}");
                                }

                                croppedLabels.Add(new CroppedLabel
                                {
                                    Id = i + 1,
                                    Image = cropped,
                                    Corners = RectangleToCorners(rect),
                                    RotationAngle = 0,
                                    FilePath = outputPath,
                                    DetectionMethod = "Template Matching"
                                });
                            }
                        }
                    }*/
                    else
                    {
                        Console.WriteLine("לא נמצאו תוויות / No labels found");
                    }
                

                return croppedLabels;
            }

            /// <summary>
            /// חתוך תווית עם תיקון פרספקטיבה (תומך בסיבוב)
            /// Crop label with perspective correction (supports rotation)
            /// </summary>
            private static Mat CropLabelWithPerspective(Mat sourceImage, PointF[] corners)
            {
                // סדר פינות
                // Order corners
                PointF[] orderedCorners = GlobalDetector.OrderCorners(corners);

                // חשב גודל פלט
                // Calculate output size
                float width1 = GlobalDetector.Distance(orderedCorners[0], orderedCorners[1]);
                float width2 = GlobalDetector.Distance(orderedCorners[2], orderedCorners[3]);
                float height1 = GlobalDetector.Distance(orderedCorners[0], orderedCorners[3]);
                float height2 = GlobalDetector.Distance(orderedCorners[1], orderedCorners[2]);

                int outputWidth = (int)Math.Max(width1, width2);
                int outputHeight = (int)Math.Max(height1, height2);

                // הגדר נקודות יעד (מלבן ישר)
                // Define destination points (straight rectangle)
                PointF[] destPoints = new PointF[]
                {
                new PointF(0, 0),
                new PointF(outputWidth - 1, 0),
                new PointF(outputWidth - 1, outputHeight - 1),
                new PointF(0, outputHeight - 1)
                };
              
            // קבל מטריצת טרנספורמציה
            // Get transformation matrix
            Mat M = CvInvoke.GetPerspectiveTransform(orderedCorners, destPoints);

                // בצע טרנספורמציה
                // Apply transformation
                Mat cropped = new Mat();
                CvInvoke.WarpPerspective(sourceImage, cropped, M, new Size(outputWidth, outputHeight));

                M.Dispose();

                return cropped;
            }

           

         /*   /// <summary>
            /// המר מלבן לפינות
            /// Convert rectangle to corners
            /// </summary>
            private static PointF[] RectangleToCorners(Rectangle rect)
            {
                return new PointF[]
                {
                new PointF(rect.Left, rect.Top),
                new PointF(rect.Right, rect.Top),
                new PointF(rect.Right, rect.Bottom),
                new PointF(rect.Left, rect.Bottom)
                };
            }

            /// <summary>
            /// סדר פינות (למעלה-שמאל, למעלה-ימין, למטה-ימין, למטה-שמאל)
            /// Order corners (top-left, top-right, bottom-right, bottom-left)
            /// </summary>
            private static PointF[] OrderCorners(PointF[] corners)
            {
                if (corners.Length != 4)
                    return corners;

                PointF[] sorted = (PointF[])corners.Clone();

                // מיין לפי סכום (x+y)
                // Sort by sum (x+y)
                Array.Sort(sorted, (a, b) => (a.X + a.Y).CompareTo(b.X + b.Y));

                PointF topLeft = sorted[0];
                PointF bottomRight = sorted[3];

                // בין 2 האמצעיים
                // Between the 2 middle ones
                PointF topRight = sorted[1].X < sorted[2].X ? sorted[1] : sorted[2];
                PointF bottomLeft = sorted[1].X < sorted[2].X ? sorted[2] : sorted[1];

                return new PointF[] { topLeft, topRight, bottomRight, bottomLeft };
            }

            /// <summary>
            /// חשב מרחק בין נקודות
            /// Calculate distance between points
            /// </summary>
            private static float Distance(PointF p1, PointF p2)
            {
                float dx = p2.X - p1.X;
                float dy = p2.Y - p1.Y;
                return (float)Math.Sqrt(dx * dx + dy * dy);
            }

            /// <summary>
            /// עיבוד אצווה - זהה וחתוך תוויות מסובבות מתמונות מרובות
            /// Batch processing - detect and crop rotated labels from multiple images
            /// </summary>
            public static void BatchDetectRotatedLabels(string templatePath, string[] imagePaths,
                string outputRootFolder)
            {
                Console.WriteLine($"\nמעבד {imagePaths.Length} תמונות...");
                Console.WriteLine($"Processing {imagePaths.Length} images...\n");

                int totalLabels = 0;

                for (int i = 0; i < imagePaths.Length; i++)
                {
                    string imagePath = imagePaths[i];
                    string imageName = System.IO.Path.GetFileNameWithoutExtension(imagePath);
                    string imageOutputFolder = System.IO.Path.Combine(outputRootFolder, imageName);

                    Console.WriteLine($"[{i + 1}/{imagePaths.Length}] {imageName}");

                    try
                    {
                        var labels = DetectAndCropRotatedLabels(templatePath, imagePath, imageOutputFolder);
                        totalLabels += labels.Count;

                        Console.WriteLine($"  ✓ נמצאו {labels.Count} תוויות");
                        Console.WriteLine($"  ✓ Found {labels.Count} labels\n");

                        // שחרר זיכרון
                        // Release memory
                        foreach (var label in labels)
                        {
                            label.Image?.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ✗ שגיאה / Error: {ex.Message}\n");
                    }

                    // ניקוי זיכרון כל 5 תמונות
                    // Memory cleanup every 5 images
                    if ((i + 1) % 5 == 0)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }

                Console.WriteLine($"\n{'=',50}");
                Console.WriteLine($"סיום עיבוד אצווה / Batch Processing Complete");
                Console.WriteLine($"{'=',50}");
                Console.WriteLine($"סה\"כ תמונות / Total images: {imagePaths.Length}");
                Console.WriteLine($"סה\"כ תוויות / Total labels: {totalLabels}");
            }*/

            /// <summary>
            /// הצג מידע על תווית שנחתכה
            /// Display information about cropped label
            /// </summary>
            public static void PrintLabelInfo(CroppedLabel label)
            {
                Console.WriteLine($"\nתווית #{label.Id} / Label #{label.Id}");
                Console.WriteLine($"  גודל / Size: {label.Image.Width}x{label.Image.Height}");
                Console.WriteLine($"  זווית / Angle: {label.RotationAngle:F1}°");
                Console.WriteLine($"  שיטת זיהוי / Method: {label.DetectionMethod}");

                if (!string.IsNullOrEmpty(label.FilePath))
                    Console.WriteLine($"  קובץ / File: {label.FilePath}");

                Console.WriteLine($"  פינות / Corners:");
                for (int i = 0; i < label.Corners.Length && i < 4; i++)
                {
                    Console.WriteLine($"    [{i}]: ({label.Corners[i].X:F1}, {label.Corners[i].Y:F1})");
                }
            }

    /// <summary>
    /// עיבוד אצווה - זהה וחתוך תוויות מסובבות מתמונות מרובות
    /// Batch processing - detect and crop rotated labels from multiple images
    /// </summary>
    public static void BatchDetectRotatedLabels(//string templatePath, string[] imagePaths,
                                       string imageName,
                                       string imageOutputFolder, 
                                       List<StickerDetection> detections ,
                                       Mat sourceImage )
    {
       // Console.WriteLine($"\nמעבד {imagePaths.Length} תמונות...");
       // Console.WriteLine($"Processing {imagePaths.Length} images...\n");

        int totalLabels = 0;

      /*  for (int i = 0; i < imagePaths.Length; i++)
        {
            string imagePath = imagePaths[i];
            string imageName = System.IO.Path.GetFileNameWithoutExtension(imagePath);
            string imageOutputFolder = System.IO.Path.Combine(outputRootFolder, imageName);

            Console.WriteLine($"[{i + 1}/{imagePaths.Length}] {imageName}");
      */
            try
            {
                var labels = DetectAndCropRotatedLabels(imageOutputFolder ,Path.GetFileNameWithoutExtension( imageName), sourceImage, detections);
                totalLabels += labels.Count;

                Console.WriteLine($"  ✓ נמצאו {labels.Count} תוויות");
                Console.WriteLine($"  ✓ Found {labels.Count} labels\n");

                // שחרר זיכרון
                // Release memory
                foreach (var label in labels)
                {
                    label.Image?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ שגיאה / Error: {ex.Message}\n");
            }

            // ניקוי זיכרון כל 5 תמונות
            // Memory cleanup every 5 images
          //  if ((i + 1) % 5 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
       /* }

        Console.WriteLine($"\n{'=',50}");
        Console.WriteLine($"סיום עיבוד אצווה / Batch Processing Complete");
        Console.WriteLine($"{'=',50}");
        Console.WriteLine($"סה\"כ תמונות / Total images: {imagePaths.Length}");
        Console.WriteLine($"סה\"כ תוויות / Total labels: {totalLabels}");*/
    }
}



    
}

