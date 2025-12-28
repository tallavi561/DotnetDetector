using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace FindLabel
{
    /// <summary>
    /// Specialized detector for WHITE rectangles on GRAY backgrounds
    /// מזהה מיוחד למלבנים לבנים על רקע אפור
    /// </summary>
    public class OpenCVLabelSigner2
    {
        private const int MIN_LABEL_AREA = 8000;
        private const int MAX_LABEL_AREA = 50000000;
        private const double MIN_RECTANGULARITY = 0.75;  // Must be 75% rectangular
        private const int CANNY_THRESHOLD1 = 50;
        private const int CANNY_THRESHOLD2 = 150;
        private const bool SAVE_DEBUG_IMAGES = true;

       

        public static void ProcessImage(string inputPath, string outputPath, string signatureText)
        {
            using (Mat image = CvInvoke.Imread(inputPath, ImreadModes.AnyColor))
            {
                if (image.IsEmpty)
                    throw new Exception("Cannot load image");

                Console.WriteLine($"📐 Image: {image.Width}x{image.Height}");
                Console.WriteLine($"📊 Channels: {image.NumberOfChannels}");
                Console.WriteLine();

                // Convert to grayscale
                Mat gray = new Mat();
                if (image.NumberOfChannels == 1)
                {
                    image.CopyTo(gray);
                }
                else
                {
                    CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);
                }

                List<Rectangle> allLabels = new List<Rectangle>();

                // Method 1: Edge detection to find rectangles
                Console.WriteLine("🔍 Method 1: Canny edge detection...");
                var labels1 = DetectWithEdges(gray);
                Console.WriteLine($"   Found: {labels1.Count} rectangles");
                allLabels.AddRange(labels1);

                // Method 2: Look for bright regions that are rectangular
                Console.WriteLine("🔍 Method 2: Bright rectangular regions...");
                var labels2 = DetectBrightRectangles(gray);
                Console.WriteLine($"   Found: {labels2.Count} rectangles");
                allLabels.AddRange(labels2);

                // Method 3: Adaptive threshold looking for white
                Console.WriteLine("🔍 Method 3: Adaptive white detection...");
                var labels3 = DetectAdaptiveWhite(gray);
                Console.WriteLine($"   Found: {labels3.Count} rectangles");
                allLabels.AddRange(labels3);

                // Merge and filter
                Console.WriteLine();
                Console.WriteLine("🔄 Merging and filtering...");
                List<Rectangle> finalLabels = MergeAndFilter(allLabels, gray);

                Console.WriteLine();
                Console.WriteLine($"✅ Detected {finalLabels.Count} white rectangles");
                Console.WriteLine();

                if (finalLabels.Count == 0)
                {
                    Console.WriteLine("⚠️  No rectangles found!");
                    Console.WriteLine("💡 Check debug images:");
                    Console.WriteLine("   - debug_edges.jpg");
                    Console.WriteLine("   - debug_bright.jpg");
                    Console.WriteLine("   - debug_adaptive.jpg");
                }

                // Draw results
                Mat outputImage;
                if (image.NumberOfChannels == 1)
                {
                    outputImage = new Mat();
                    CvInvoke.CvtColor(image, outputImage, ColorConversion.Gray2Bgr);
                }
                else
                {
                    outputImage = image.Clone();
                }

                using (Bitmap bitmap = outputImage.ToBitmap())
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    for (int i = 0; i < finalLabels.Count; i++)
                    {
                        Rectangle label = finalLabels[i];
                        Console.WriteLine($"📦 Rectangle {i + 1}:");
                        Console.WriteLine($"   Position: ({label.X}, {label.Y})");
                        Console.WriteLine($"   Size: {label.Width} x {label.Height}");
                        Console.WriteLine($"   Area: {label.Width * label.Height:N0} px²");

                        DrawSignature(g, label, signatureText, i + 1);
                    }

                    bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                outputImage.Dispose();
                gray.Dispose();
            }
        }

        private static List<Rectangle> DetectWithEdges(Mat gray)
        {
            using (Mat blurred = new Mat())
            using (Mat edges = new Mat())
            {
                // Blur to reduce noise
                CvInvoke.GaussianBlur(gray, blurred, new Size(5, 5), 0);

                // Canny edge detection
                CvInvoke.Canny(blurred, edges, CANNY_THRESHOLD1, CANNY_THRESHOLD2);

                // Dilate edges to connect nearby edges
                Mat kernel = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, new Size(3, 3), new Point(-1, -1));
                CvInvoke.Dilate(edges, edges, kernel, new Point(-1, -1), 2, BorderType.Default, new MCvScalar());
                kernel.Dispose();

                if (SAVE_DEBUG_IMAGES)
                    CvInvoke.Imwrite("debug_edges.jpg", edges);

                return FindRectangularContours(edges, gray);
            }
        }

        private static List<Rectangle> DetectBrightRectangles(Mat gray)
        {
            using (Mat blurred = new Mat())
            using (Mat bright = new Mat())
            {
                CvInvoke.GaussianBlur(gray, blurred, new Size(5, 5), 0);

                // Find pixels brighter than the median
                double minVal = 0.0 , maxVal = 0.0 ;
                Point minLoc =new Point(), maxLoc = new Point();
                CvInvoke.MinMaxLoc(blurred, ref minVal, ref  maxVal,  ref minLoc, ref maxLoc);

                double threshold = minVal + (maxVal - minVal) * 0.6; // Top 40% brightness

                Console.WriteLine($"   Brightness range: {minVal:F0} - {maxVal:F0}");
                Console.WriteLine($"   Using threshold: {threshold:F0}");

                CvInvoke.Threshold(blurred, bright, threshold, 255, ThresholdType.Binary);

                // Clean up with morphology
                Mat kernel = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, new Size(5, 5), new Point(-1, -1));
                CvInvoke.MorphologyEx(bright, bright, MorphOp.Close, kernel, new Point(-1, -1), 2, BorderType.Default, new MCvScalar());
                CvInvoke.MorphologyEx(bright, bright, MorphOp.Open, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
                kernel.Dispose();

                if (SAVE_DEBUG_IMAGES)
                    CvInvoke.Imwrite("debug_bright.jpg", bright);

                return FindRectangularContours(bright, gray);
            }
        }

        private static List<Rectangle> DetectAdaptiveWhite(Mat gray)
        {
            using (Mat blurred = new Mat())
            using (Mat adaptive = new Mat())
            {
                CvInvoke.GaussianBlur(gray, blurred, new Size(5, 5), 0);

                // Adaptive threshold - looks for locally bright regions
                CvInvoke.AdaptiveThreshold(blurred, adaptive, 255,
                    AdaptiveThresholdType.GaussianC,
                    ThresholdType.Binary,
                    51, -5);

                // Morphology to find solid rectangles
                Mat kernel = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, new Size(7, 7), new Point(-1, -1));
                CvInvoke.MorphologyEx(adaptive, adaptive, MorphOp.Close, kernel, new Point(-1, -1), 3, BorderType.Default, new MCvScalar());
                kernel.Dispose();

                if (SAVE_DEBUG_IMAGES)
                    CvInvoke.Imwrite("debug_adaptive.jpg", adaptive);

                return FindRectangularContours(adaptive, gray);
            }
        }

        private static List<Rectangle> FindRectangularContours(Mat binary, Mat originalGray)
        {
            List<Rectangle> rectangles = new List<Rectangle>();

            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            using (Mat hierarchy = new Mat())
            {
                CvInvoke.FindContours(binary, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                for (int i = 0; i < contours.Size; i++)
                {
                    using (VectorOfPoint contour = contours[i])
                    {
                        double area = CvInvoke.ContourArea(contour);

                        if (area < MIN_LABEL_AREA || area > MAX_LABEL_AREA)
                            continue;

                        // Get bounding rectangle
                        Rectangle rect = CvInvoke.BoundingRectangle(contour);

                        // Check if it's rectangular enough
                        double rectangularity = area / (rect.Width * rect.Height);
                        if (rectangularity < MIN_RECTANGULARITY)
                            continue;

                        // Check aspect ratio - labels are typically not too extreme
                        double aspectRatio = (double)rect.Width / rect.Height;
                        if (aspectRatio < 0.3 || aspectRatio > 5.0)
                            continue;

                        // Verify it has 4 corners (approximately)
                        using (VectorOfPoint approx = new VectorOfPoint())
                        {
                            double perimeter = CvInvoke.ArcLength(contour, true);
                            CvInvoke.ApproxPolyDP(contour, approx, 0.04 * perimeter, true);

                            // Should have 4-8 corners for a rectangle
                            if (approx.Size >= 4 && approx.Size <= 8)
                            {
                                rectangles.Add(rect);
                            }
                        }
                    }
                }
            }

            return rectangles;
        }

        private static List<Rectangle> MergeAndFilter(List<Rectangle> labels, Mat gray)
        {
            if (labels.Count == 0) return labels;

            Console.WriteLine($"   Starting with {labels.Count} regions");

            // Remove duplicates
            labels = labels.Distinct().ToList();

            // Merge overlapping
            bool changed = true;
            int iterations = 0;
            while (changed && iterations < 10)
            {
                changed = false;
                iterations++;
                List<Rectangle> merged = new List<Rectangle>();
                bool[] used = new bool[labels.Count];

                for (int i = 0; i < labels.Count; i++)
                {
                    if (used[i]) continue;

                    Rectangle current = labels[i];

                    for (int j = i + 1; j < labels.Count; j++)
                    {
                        if (used[j]) continue;

                        Rectangle other = labels[j];
                        Rectangle expanded1 = current;
                        expanded1.Inflate(20, 20);

                        if (expanded1.IntersectsWith(other))
                        {
                            current = Rectangle.Union(current, other);
                            used[j] = true;
                            changed = true;
                        }
                    }

                    merged.Add(current);
                    used[i] = true;
                }

                labels = merged;
            }

            Console.WriteLine($"   After merging: {labels.Count} regions");

            // Expand slightly to ensure we capture the whole label
            List<Rectangle> expanded = new List<Rectangle>();
            foreach (var label in labels)
            {
                Rectangle exp = label;
                exp.Inflate(10, 10);

                exp.X = Math.Max(0, exp.X);
                exp.Y = Math.Max(0, exp.Y);
                exp.Width = Math.Min(gray.Width - exp.X, exp.Width);
                exp.Height = Math.Min(gray.Height - exp.Y, exp.Height);

                expanded.Add(exp);
            }
            labels = expanded;

            // Final filter by area
            labels = labels.Where(r =>
            {
                int area = r.Width * r.Height;
                return area >= MIN_LABEL_AREA && area <= MAX_LABEL_AREA;
            }).ToList();

            // Sort by position
            labels = labels.OrderBy(r => r.Y).ThenBy(r => r.X).ToList();

            return labels;
        }

        private static void DrawSignature(Graphics g, Rectangle label, string text, int number)
        {
            // Red border
            using (Pen pen = new Pen(Color.FromArgb(230, 255, 0, 0), 6))
            {
                g.DrawRectangle(pen, label);
            }

            // Number badge
            using (Font numFont = new Font("Arial", 20, FontStyle.Bold))
            using (Brush numBrush = new SolidBrush(Color.White))
            using (Brush badgeBrush = new SolidBrush(Color.FromArgb(230, 255, 0, 0)))
            {
                string num = number.ToString();
                SizeF numSize = g.MeasureString(num, numFont);
                float badgeSize = Math.Max(numSize.Width, numSize.Height) + 20;

                RectangleF badge = new RectangleF(label.X + 10, label.Y + 10, badgeSize, badgeSize);
                g.FillEllipse(badgeBrush, badge);

                float textX = badge.X + (badge.Width - numSize.Width) / 2;
                float textY = badge.Y + (badge.Height - numSize.Height) / 2;
                g.DrawString(num, numFont, numBrush, textX, textY);
            }

            // Checkmark
            int size = 40;
            int x = label.Right - size - 20;
            int y = label.Top + 20;

            using (Brush circleBg = new SolidBrush(Color.FromArgb(230, 0, 200, 0)))
            {
                g.FillEllipse(circleBg, x - 10, y - 10, size + 20, size + 20);
            }

            using (Pen checkPen = new Pen(Color.White, 5))
            {
                checkPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                checkPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                Point[] points = new Point[]
                {
                    new Point(x + size/4, y + size/2),
                    new Point(x + size/2, y + size*3/4),
                    new Point(x + size*7/8, y + size/4)
                };

                g.DrawLines(checkPen, points);
            }

            // Signature text
            int fontSize = Math.Max(14, Math.Min(24, label.Height / 10));
            using (Font font = new Font("Arial", fontSize, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(text, font);
                float tx = label.X + (label.Width - textSize.Width) / 2;
                float ty = label.Y + label.Height - textSize.Height - 15;

                RectangleF textBg = new RectangleF(tx - 10, ty - 5, textSize.Width + 20, textSize.Height + 10);

                using (Brush bgBrush = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
                {
                    g.FillRectangle(bgBrush, textBg);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(230, 255, 0, 0), 2))
                {
                    g.DrawRectangle(borderPen, Rectangle.Round(textBg));
                }

                using (Brush textBrush = new SolidBrush(Color.FromArgb(230, 200, 0, 0)))
                {
                    g.DrawString(text, font, textBrush, tx, ty);
                }
            }
        }
    }
}