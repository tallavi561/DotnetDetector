using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV;

namespace FindLabel
{
    public  class PackageLabelSigner
    {
        private const int MIN_CONTOUR_AREA = 15000;      // שטח מינימלי של קונטור
        private const int MAX_CONTOUR_AREA = 500000;     // שטח מקסימלי
        private const double MIN_RECT_AREA = 10000;      // שטח מינימלי של מלבן
        private const int BLUR_SIZE = 5;                 // גודל טשטוש
        private const int THRESHOLD_VALUE = 150;         // ערך סף
        private const int MORPH_SIZE = 3;                // גודל פעולות מורפולוגיות
        /*
        public static void ProcessImage(string inputPath, string outputPath, string signatureText)
        {
            // Load image with OpenCV
            using (Mat image = CvInvoke.Imread(inputPath, ImreadModes.AnyColor))
            {
                if (image.IsEmpty)
                {
                    throw new Exception("לא ניתן לטעון את התמונה / Cannot load image");
                }

                Console.WriteLine($"📐 גודל תמונה / Image size: {image.Width}x{image.Height}");
                Console.WriteLine($"📊 ערוצים / Channels: {image.NumberOfChannels}");
                Console.WriteLine();

                // Step 1: Convert to grayscale
                Console.WriteLine("🔄 שלב 1: המרה לגווני אפור / Converting to grayscale...");
                using (Mat gray = new Mat())
                {
                    CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);

                    // Step 2: Apply Gaussian Blur to reduce noise
                    Console.WriteLine("🌫️  שלב 2: טשטוש לסינון רעש / Applying Gaussian blur...");
                    using (Mat blurred = new Mat())
                    {
                        CvInvoke.GaussianBlur(gray, blurred, new Size(BLUR_SIZE, BLUR_SIZE), 0);

                        // Step 3: Apply adaptive threshold
                        Console.WriteLine("⚖️  שלב 3: סף אדפטיבי / Applying adaptive threshold...");
                        using (Mat thresh = new Mat())
                        {
                            // Try multiple thresholding methods
                            CvInvoke.Threshold(blurred, thresh, THRESHOLD_VALUE, 255, ThresholdType.Binary);

                            // Step 4: Morphological operations to clean up
                            Console.WriteLine("🔧 שלב 4: פעולות מורפולוגיות / Morphological operations...");
                            using (Mat kernel = CvInvoke.GetStructuringElement(
                                MorphShapes.Rectangle,
                                new Size(MORPH_SIZE, MORPH_SIZE),
                                new Point(-1, -1)))
                            {
                                using (Mat morphed = new Mat())
                                {
                                    // Close small holes
                                    CvInvoke.MorphologyEx(thresh, morphed, MorphOp.Close, kernel, new Point(-1, -1), 2, BorderType.Default, new MCvScalar());

                                    // Remove small noise
                                    CvInvoke.MorphologyEx(morphed, morphed, MorphOp.Open, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

                                    // Step 5: Find contours
                                    Console.WriteLine("🔍 שלב 5: חיפוש קווי מתאר / Finding contours...");
                                    using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                                    {
                                        using (Mat hierarchy = new Mat())
                                        {
                                            CvInvoke.FindContours(morphed, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                                            Console.WriteLine($"   ↳ נמצאו {contours.Size} קווי מתאר / Found {contours.Size} contours");

                                            // Step 6: Filter and process contours
                                            List<Rectangle> labels = FilterContours(contours);

                                            Console.WriteLine();
                                            Console.WriteLine($"✅ זוהו {labels.Count} מדבקות / Detected {labels.Count} labels");
                                            Console.WriteLine();

                                            if (labels.Count == 0)
                                            {
                                                Console.WriteLine("⚠️  לא נמצאו מדבקות. נסה:");
                                                Console.WriteLine("   • להוריד את THRESHOLD_VALUE (כרגע: " + THRESHOLD_VALUE + ")");
                                                Console.WriteLine("   • להוריד את MIN_CONTOUR_AREA (כרגע: " + MIN_CONTOUR_AREA + ")");
                                                Console.WriteLine();
                                                Console.WriteLine("⚠️  No labels found. Try:");
                                                Console.WriteLine("   • Lower THRESHOLD_VALUE (current: " + THRESHOLD_VALUE + ")");
                                                Console.WriteLine("   • Lower MIN_CONTOUR_AREA (current: " + MIN_CONTOUR_AREA + ")");
                                            }

                                            // Convert OpenCV Mat to Bitmap for drawing
                                            using (Bitmap bitmap = image.ToBitmap())
                                            using (Graphics g = Graphics.FromImage(bitmap))
                                            {
                                                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                                                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                                                // Draw signatures
                                                for (int i = 0; i < labels.Count; i++)
                                                {
                                                    Rectangle label = labels[i];
                                                    Console.WriteLine($"   מדבקה {i + 1} / Label {i + 1}:");
                                                    Console.WriteLine($"      מיקום / Position: ({label.X}, {label.Y})");
                                                    Console.WriteLine($"      גודל / Size: {label.Width}x{label.Height}");
                                                    Console.WriteLine($"      שטח / Area: {label.Width * label.Height:N0} pixels");

                                                    SignLabel(g, label, signatureText, i + 1);
                                                }

                                                // Save result
                                                bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static List<Rectangle> FilterContours(VectorOfVectorOfPoint contours)
        {
            List<Rectangle> labels = new List<Rectangle>();

            for (int i = 0; i < contours.Size; i++)
            {
                using (VectorOfPoint contour = contours[i])
                {
                    double area = CvInvoke.ContourArea(contour);

                    // Filter by area
                    if (area < MIN_CONTOUR_AREA || area > MAX_CONTOUR_AREA)
                        continue;

                    // Get bounding rectangle
                    Rectangle boundingRect = CvInvoke.BoundingRectangle(contour);

                    // Filter by rectangle area
                    if (boundingRect.Width * boundingRect.Height < MIN_RECT_AREA)
                        continue;

                    // Filter by aspect ratio (labels are usually rectangular)
                    double aspectRatio = (double)boundingRect.Width / boundingRect.Height;
                    if (aspectRatio < 0.3 || aspectRatio > 10.0)
                        continue;

                    // Check if contour is approximately rectangular
                    using (VectorOfPoint approx = new VectorOfPoint())
                    {
                        double perimeter = CvInvoke.ArcLength(contour, true);
                        CvInvoke.ApproxPolyDP(contour, approx, 0.02 * perimeter, true);

                        // Labels typically have 4-8 corners after approximation
                        if (approx.Size >= 4 && approx.Size <= 12)
                        {
                            labels.Add(boundingRect);
                        }
                    }
                }
            }

            // Merge overlapping rectangles
            labels = MergeOverlapping(labels);

            return labels;
        }

        private static List<Rectangle> MergeOverlapping(List<Rectangle> rectangles)
        {
            if (rectangles.Count <= 1) return rectangles;

            List<Rectangle> merged = new List<Rectangle>();
            bool[] used = new bool[rectangles.Count];

            for (int i = 0; i < rectangles.Count; i++)
            {
                if (used[i]) continue;

                Rectangle current = rectangles[i];

                for (int j = i + 1; j < rectangles.Count; j++)
                {
                    if (used[j]) continue;

                    Rectangle other = rectangles[j];
                    Rectangle expanded = current;
                    expanded.Inflate(30, 30);

                    if (expanded.IntersectsWith(other))
                    {
                        current = Rectangle.Union(current, other);
                        used[j] = true;
                    }
                }

                merged.Add(current);
                used[i] = true;
            }

            return merged;
        }

        private static void SignLabel(Graphics g, Rectangle label, string signatureText, int labelNumber)
        {
            // Draw outer border (red)
            using (Pen outerPen = new Pen(Color.FromArgb(220, 255, 0, 0), 5))
            {
                g.DrawRectangle(outerPen, label);
            }

            // Draw inner border (lighter red)
            Rectangle innerRect = label;
            innerRect.Inflate(-8, -8);
            using (Pen innerPen = new Pen(Color.FromArgb(180, 255, 80, 80), 3))
            {
                g.DrawRectangle(innerPen, innerRect);
            }

            // Draw label number in top-left corner
            using (Font numberFont = new Font("Arial", 16, FontStyle.Bold))
            using (Brush numberBrush = new SolidBrush(Color.White))
            using (Brush numberBg = new SolidBrush(Color.FromArgb(200, 255, 0, 0)))
            {
                string number = labelNumber.ToString();
                SizeF numberSize = g.MeasureString(number, numberFont);
                RectangleF numberRect = new RectangleF(
                    label.X + 10,
                    label.Y + 10,
                    numberSize.Width + 16,
                    numberSize.Height + 8);

                g.FillEllipse(numberBg, numberRect);
                g.DrawString(number, numberFont, numberBrush, numberRect.X + 8, numberRect.Y + 4);
            }

            // Draw checkmark in top-right
            DrawCheckmark(g, label);

            // Draw signature text at bottom
            int fontSize = Math.Max(14, Math.Min(28, label.Height / 10));
            using (Font font = new Font("Arial", fontSize, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(signatureText, font);

                float x = label.X + (label.Width - textSize.Width) / 2;
                float y = label.Y + label.Height - textSize.Height - 15;

                // Ensure text stays within bounds
                x = Math.Max(label.X + 15, Math.Min(x, label.X + label.Width - textSize.Width - 15));
                y = Math.Max(label.Y + 15, y);

                RectangleF textRect = new RectangleF(x - 12, y - 6, textSize.Width + 24, textSize.Height + 12);

                // Shadow
                RectangleF shadowRect = textRect;
                shadowRect.Offset(3, 3);
                using (Brush shadowBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                {
                    GraphicsExtensions.FillRoundedRectangle(g, shadowBrush, shadowRect, 8);
                }

                // Background
                using (Brush bgBrush = new SolidBrush(Color.FromArgb(250, 255, 255, 255)))
                {
                    GraphicsExtensions.FillRoundedRectangle(g, bgBrush, textRect, 8);
                }

                // Border
                using (Pen borderPen = new Pen(Color.FromArgb(220, 255, 0, 0), 3))
                {
                    GraphicsExtensions.DrawRoundedRectangle(g, borderPen, textRect, 8);
                }

                // Text
                using (Brush textBrush = new SolidBrush(Color.FromArgb(255, 200, 0, 0)))
                {
                    g.DrawString(signatureText, font, textBrush, x, y);
                }
            }
        }

        private static void DrawCheckmark(Graphics g, Rectangle label)
        {
            int size = Math.Min(50, Math.Min(label.Height / 6, label.Width / 6));
            int x = label.Right - size - 20;
            int y = label.Top + 20;

            // Background circle
            using (Brush bgBrush = new SolidBrush(Color.FromArgb(230, 0, 200, 0)))
            {
                g.FillEllipse(bgBrush, x - 10, y - 10, size + 20, size + 20);
            }

            // White border
            using (Pen borderPen = new Pen(Color.White, 4))
            {
                g.DrawEllipse(borderPen, x - 10, y - 10, size + 20, size + 20);
            }

            // Checkmark
            using (Pen checkPen = new Pen(Color.White, 5))
            {
                checkPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                checkPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                checkPen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;

                Point[] checkmark = new Point[]
                {
                    new Point(x + size/4, y + size/2),
                    new Point(x + size/2, y + size*3/4),
                    new Point(x + size*7/8, y + size/5)
                };

                g.DrawLines(checkPen, checkmark);
            }
        }
    }

    // Helper methods for rounded rectangles
    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(Graphics g, Brush brush, RectangleF rect, float radius)
        {
            using (var path = GetRoundedRect(rect, radius))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRectangle(Graphics g, Pen pen, RectangleF rect, float radius)
        {
            using (var path = GetRoundedRect(rect, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        private static System.Drawing.Drawing2D.GraphicsPath GetRoundedRect(RectangleF rect, float radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            float diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }*/
    }
}
