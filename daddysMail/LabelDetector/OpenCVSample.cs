using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace FindLabel
{
    public  class OpenCVSample
    {
       

        public static void Main(string input)
            {
                // שימוש:
                // LabelDetectAndDeskew.exe "C:\imgs" "C:\out"
               
                string outDir = Path.GetDirectoryName(input)+"\\";

                /*Directory.CreateDirectory(outDir);
                Directory.CreateDirectory(Path.Combine(outDir, "crops_aligned"));

                string[] files = Directory.Exists(input)
                    ? Directory.GetFiles(input, "*.*", SearchOption.TopDirectoryOnly)
                    : new[] { input };

                foreach (var file in files)
                {*/
                   

                    using var gray = Cv2.ImRead(input, ImreadModes.Grayscale);
                   

                    var detections = DetectLabels(gray);

                    using var vis = new Mat();
                    Cv2.CvtColor(gray, vis, ColorConversionCodes.GRAY2BGR);

                    int idx = 0;
                    foreach (var det in detections)
                    {
                        // 1) מסמן בתמונה
                        DrawRotatedRect(vis, det.Rect, new Scalar(0, 255, 0), 2);

                        // 2) מיישר ומוציא CROP
                        using var aligned = DeskewLabel(gray, det.Rect);
                        if (!aligned.Empty())
                        {
                            // אופציונלי: שיפור קטן ל-OCR/ברקוד
                            using var cleaned = ImproveForText(aligned);

                            string outCrop = Path.Combine(outDir, "crops_aligned",
                                $"{Path.GetFileNameWithoutExtension(input)}_label_{idx:D2}.png");
                            Cv2.ImWrite(outCrop, cleaned);
                        }

                        idx++;
                    }

                    string outMarked = Path.Combine(outDir,
                        $"{Path.GetFileNameWithoutExtension(input)}_marked.png");
                    Cv2.ImWrite(outMarked, vis);

                    Console.WriteLine($"{Path.GetFileName(input)} -> found {detections.Count} labels");
                }

              /*  Console.WriteLine("Done.");
            }*/

            // ------------------- Detection -------------------

            private static List<Detection> DetectLabels(Mat gray)
            {
                // 1) Blur + CLAHE
                using var blur = new Mat();
                Cv2.GaussianBlur(gray, blur, new Size(5, 5), 0);

                using var claheOut = new Mat();
                using (var clahe = Cv2.CreateCLAHE(clipLimit: 2.5, tileGridSize: new Size(8, 8)))
                    clahe.Apply(blur, claheOut);

                // 2) Threshold על תמונה הפוכה: מדבקה לבנה הופכת ל"אובייקט"
                using var inv = new Mat();
                Cv2.BitwiseNot(claheOut, inv);

                using var bin = new Mat();
                Cv2.AdaptiveThreshold(inv, bin, 255,
                    AdaptiveThresholdTypes.GaussianC,
                    ThresholdTypes.Binary,
                    blockSize: 35,
                    c: 5);

                // 3) Close/Open כדי לאחד את המדבקה למרות טקסט וברקודים
                using var kClose = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(15, 15));
                using var morph = new Mat();
                Cv2.MorphologyEx(bin, morph, MorphTypes.Close, kClose, iterations: 2);

                using var kOpen = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
                Cv2.MorphologyEx(morph, morph, MorphTypes.Open, kOpen, iterations: 1);

                // 4) Contours
                Cv2.FindContours(morph, out Point[][] contours, out _, RetrievalModes.External,
                    ContourApproximationModes.ApproxSimple);

                var results = new List<Detection>();
                double imgArea = gray.Rows * gray.Cols;

                foreach (var c in contours)
                {
                    double area = Cv2.ContourArea(c);
                    if (area < imgArea * 0.002) continue;  // קטן מדי
                    if (area > imgArea * 0.35) continue;   // גדול מדי

                    var rr = Cv2.MinAreaRect(c);
                    double w = rr.Size.Width;
                    double h = rr.Size.Height;
                    if (w < 20 || h < 20) continue;

                    double rectArea = w * h;
                    if (rectArea <= 0) continue;

                    double fill = area / rectArea;
                    if (fill < 0.55) continue;

                    double aspect = Math.Max(w, h) / Math.Min(w, h);
                    if (aspect > 8.0) continue;

                    // 5) אימות: המדבקה בהירה + יש "טקסט שחור" בפנים
                    if (!LooksLikeWhiteLabelWithBlackText(gray, rr)) continue;

                    results.Add(new Detection(rr, area, fill, aspect));
                }

                return results;
            }

            private static bool LooksLikeWhiteLabelWithBlackText(Mat gray, RotatedRect rr)
            {
                using var aligned = DeskewLabel(gray, rr);
                if (aligned.Empty()) return false;

                // חתוך שוליים כדי להימנע מהרקע/פלסטיק מסביב
                int padX = (int)(aligned.Cols * 0.06);
                int padY = (int)(aligned.Rows * 0.06);
                var innerRect = new Rect(
                    Math.Clamp(padX, 0, aligned.Cols - 1),
                    Math.Clamp(padY, 0, aligned.Rows - 1),
                    Math.Clamp(aligned.Cols - 2 * padX, 1, aligned.Cols),
                    Math.Clamp(aligned.Rows - 2 * padY, 1, aligned.Rows));

                using var inner = new Mat(aligned, innerRect);

                double mean = Cv2.Mean(inner).Val0;
                if (mean < 120) return false; // לא מספיק "לבן"

                // אחוז פיקסלים כהים (טקסט/ברקוד)
                using var darkMask = new Mat();
                double darkThr = Math.Max(70, Math.Min(90, mean - 35));
                Cv2.Threshold(inner, darkMask, darkThr, 255, ThresholdTypes.BinaryInv);
                double darkRatio = (double)Cv2.CountNonZero(darkMask) / (inner.Rows * inner.Cols);

                if (darkRatio < 0.015) return false;  // אין מספיק טקסט
                if (darkRatio > 0.35) return false;   // יותר מדי שחור -> כנראה לא מדבקה נקייה

                // קצוות מינימליים (תוכן)
                using var edges = new Mat();
                Cv2.Canny(inner, edges, 40, 120);
                double edgeRatio = (double)Cv2.CountNonZero(edges) / (inner.Rows * inner.Cols);
                if (edgeRatio < 0.01) return false;

                return true;
            }

            // ------------------- Deskew / Crop -------------------

            public static Mat DeskewLabel(Mat gray, RotatedRect rr)
            {
                Point2f[] srcPts = rr.Points();
                Point2f[] ordered = OrderPoints(srcPts);

                float widthA = Distance(ordered[2], ordered[3]);
                float widthB = Distance(ordered[1], ordered[0]);
                float maxW = Math.Max(widthA, widthB);

                float heightA = Distance(ordered[1], ordered[2]);
                float heightB = Distance(ordered[0], ordered[3]);
                float maxH = Math.Max(heightA, heightB);

                if (maxW < 30 || maxH < 30)
                    return new Mat();

                Point2f[] dstPts =
                {
                new Point2f(0, 0),
                new Point2f(maxW - 1, 0),
                new Point2f(maxW - 1, maxH - 1),
                new Point2f(0, maxH - 1)
            };

                using var M = Cv2.GetPerspectiveTransform(ordered, dstPts);

                var warped = new Mat();
                Cv2.WarpPerspective(gray, warped, M, new Size((int)maxW, (int)maxH),
                    InterpolationFlags.Linear, BorderTypes.Replicate);

                // אם יצא "עומד" (גבוה מאוד), אפשר לסובב ל-landscape לנוחות OCR
                if (warped.Rows > warped.Cols * 1.2)
                {
                    var rotated = new Mat();
                    Cv2.Rotate(warped, rotated, RotateFlags.Rotate90Clockwise);
                    warped.Dispose();
                    return rotated;
                }

                return warped;
            }

            private static Point2f[] OrderPoints(Point2f[] pts)
            {
                Point2f tl = pts[0], tr = pts[0], br = pts[0], bl = pts[0];
                float minSum = float.MaxValue, maxSum = float.MinValue;
                float minDiff = float.MaxValue, maxDiff = float.MinValue;

                foreach (var p in pts)
                {
                    float sum = p.X + p.Y;
                    float diff = p.X - p.Y;

                    if (sum < minSum) { minSum = sum; tl = p; }
                    if (sum > maxSum) { maxSum = sum; br = p; }
                    if (diff < minDiff) { minDiff = diff; tr = p; }
                    if (diff > maxDiff) { maxDiff = diff; bl = p; }
                }

                return new[] { tl, tr, br, bl };
            }

            private static float Distance(Point2f a, Point2f b)
            {
                float dx = a.X - b.X;
                float dy = a.Y - b.Y;
                return (float)Math.Sqrt(dx * dx + dy * dy);
            }

            // ------------------- Output helpers -------------------

            private static void DrawRotatedRect(Mat imgBgr, RotatedRect rr, Scalar color, int thickness)
            {
                var pts = rr.Points();
                for (int i = 0; i < 4; i++)
                    Cv2.Line(imgBgr, (Point)pts[i], (Point)pts[(i + 1) % 4], color, thickness);
            }

            private static Mat ImproveForText(Mat alignedGray)
            {
                // שיפור עדין: נרמול + unsharp קטן + סף Otsu לטקסט
                using var norm = new Mat();
                Cv2.Normalize(alignedGray, norm, 0, 255, NormTypes.MinMax);

                using var blur = new Mat();
                Cv2.GaussianBlur(norm, blur, new Size(0, 0), 1.2);

                using var sharp = new Mat();
                Cv2.AddWeighted(norm, 1.5, blur, -0.5, 0, sharp);

                var outImg = new Mat();
                Cv2.Threshold(sharp, outImg, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                return outImg;
            }

            private static bool IsImageFile(string path)
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".tif" || ext == ".tiff";
            }

            // ------------------- Types -------------------

            private record Detection(RotatedRect Rect, double Area, double FillRatio, double AspectRatio);
        }
    }



