using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
namespace FindLabel
{
    public  class PipeLine
    {



        public static string Main(string inputPath)
        {
            // ====== Inputs ======
            string location = Path.GetDirectoryName(inputPath) + "\\";
            string patternPath = location +  "pattern.jpg";  // תבנית המדבקה (כמו ששלחת)
            string outputPath = location + "out_marked.jpg";
            string cropsDir = location + "crops";           // יישמרו כאן ה-crops המיושרים (אופציונלי)

            // ====== Tunables (מכוון לסט שלך) ======
            int brightThreshold = 190;          // 185..195 בהתאם לצילום
            int closeKernel = 21;               // 21..25 לשקיות
            double minArea = 8000;              // מינימום שטח מועמד
            double maxArea = 350000;            // מקסימום שטח מועמד
            double minRatio = 1.15;             // יחס צלעות מינימלי
            double maxRatio = 3.20;             // יחס צלעות מקסימלי

            // Template matching thresholds
            double templateThreshold = 0.70;    // 0.50..0.70; העלה אם יש false positives
            bool saveCrops = true;

            Directory.CreateDirectory(cropsDir);

            // ====== Load ======
            using Mat srcGray = CvInvoke.Imread(inputPath, ImreadModes.Grayscale);
            if (srcGray.IsEmpty) throw new FileNotFoundException("Cannot read input image: " + inputPath);

            using Mat srcBgr = new Mat();
            CvInvoke.CvtColor(srcGray, srcBgr, ColorConversion.Gray2Bgr);

            using Mat patGray = CvInvoke.Imread(patternPath, ImreadModes.Grayscale);
            if (patGray.IsEmpty) throw new FileNotFoundException("Cannot read pattern image: " + patternPath);

            // Normalize/equalize to help robustness
            using Mat srcEq = new Mat();
            using Mat patEq = new Mat();
            CvInvoke.EqualizeHist(srcGray, srcEq);
            CvInvoke.EqualizeHist(patGray, patEq);

            // ====== 1) Candidate detection with Contours ======
            using Mat bin = BuildCandidateMask(srcEq, brightThreshold, closeKernel);

            using VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(bin, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            var accepted = new List<DetectedLabel>();
            int cropIndex = 0;

            for (int i = 0; i < contours.Size; i++)
            {
                using VectorOfPoint c = contours[i];

                double area = CvInvoke.ContourArea(c);
                if (area < minArea || area > maxArea)
                    continue;

                RotatedRect rr = CvInvoke.MinAreaRect(c);

                float w = rr.Size.Width;
                float h = rr.Size.Height;
                if (w < 10 || h < 10) continue;

                float ratio = Math.Max(w, h) / Math.Min(w, h);
                if (ratio < minRatio || ratio > maxRatio)
                    continue;

                // ====== 2) Deskew crop (rotate to upright) ======
                using Mat cropUpright = ExtractUprightCrop(srcEq, rr, pad: 12);
                if (cropUpright.IsEmpty)
                    continue;

                // Preprocess crop lightly for template matching
                using Mat cropPrep = new Mat();
                CvInvoke.GaussianBlur(cropUpright, cropPrep, new Size(3, 3), 0);
                CvInvoke.EqualizeHist(cropPrep, cropPrep);

                // ====== 3) Pattern matching inside crop ======
                // Multi-scale to handle size differences
                double bestScore = -1;
                Rectangle bestRect = Rectangle.Empty;

                foreach (double scale in Scales(0.65, 1.35, 0.05))
                {
                    using Mat patScaled = ResizeMat(patEq, scale);
                    if (patScaled.Width < 20 || patScaled.Height < 20)
                        continue;

                    if (patScaled.Width >= cropPrep.Width || patScaled.Height >= cropPrep.Height)
                        continue;

                    using Mat result = new Mat();
                    CvInvoke.MatchTemplate(cropPrep, patScaled, result, TemplateMatchingType.CcoeffNormed);

                    double minVal = 0, maxVal = 0;
                    Point minLoc = new Point(), maxLoc = new Point();
                    CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                    if (maxVal > bestScore)
                    {
                        bestScore = maxVal;
                        bestRect = new Rectangle(maxLoc.X, maxLoc.Y, patScaled.Width, patScaled.Height);
                    }
                }

                // ====== 4) Accept if score is high enough ======
                if (bestScore >= templateThreshold)
                {
                    accepted.Add(new DetectedLabel(rr, bestScore));

                    if (saveCrops)
                    {
                        string cropPath = Path.Combine(cropsDir, $"label_{cropIndex:0000}_score_{bestScore.ToString("0.00", CultureInfo.InvariantCulture)}.png");
                        CvInvoke.Imwrite(cropPath, cropUpright);
                        cropIndex++;
                    }
                }
            }

            // Optional: merge duplicates (NMS on rotated rect approximated by bounding box)
            var final = NmsOnBoundingBoxes(accepted, iouThreshold: 0.25);

            // ====== Draw results ======
            foreach (var d in final)
            {
                DrawRotatedRect(srcBgr, d.Rect, new MCvScalar(0, 0, 255), 3);
                var p = Point.Round(d.Rect.Center);
                CvInvoke.PutText(srcBgr, $"{d.Score:0.00}", new Point(p.X + 10, p.Y),
                    FontFace.HersheySimplex, 0.9, new MCvScalar(0, 0, 255), 2);
            }

            CvInvoke.Imwrite(outputPath, srcBgr);
            Console.WriteLine($"Done. Found {final.Count} labels. Output: {outputPath}");
            if (saveCrops) Console.WriteLine($"Crops saved to: {Path.GetFullPath(cropsDir)}");
            return outputPath;
        }

        // ===================== Core Steps =====================

        static Mat BuildCandidateMask(Mat grayEq, int threshold, int closeKernel)
        {
            Mat work = grayEq.Clone();

            // Blur to reduce wrinkles noise
            CvInvoke.GaussianBlur(work, work, new Size(5, 5), 0);

            Mat bin = new Mat();
            CvInvoke.Threshold(work, bin, threshold, 255, ThresholdType.Binary);

            Mat kernel = CvInvoke.GetStructuringElement(
                MorphShapes.Rectangle,
                new Size(closeKernel, closeKernel),
                new Point(-1, -1));

            CvInvoke.MorphologyEx(
                bin, bin,
                MorphOp.Close,
                kernel,
                new Point(-1, -1),
                1,
                BorderType.Default,
                new MCvScalar());

            return bin;
        }

        // Deskew: extract the rotated rectangle as upright patch
        static Mat ExtractUprightCrop(Mat grayEq, RotatedRect rr, int pad)
        {
            // ensure width >= height by rotating angle accordingly
            float angle = rr.Angle;
            SizeF size = rr.Size;

            if (size.Width < size.Height)
            {
                // swap to make it upright
                angle += 90f;
                size = new SizeF(size.Height, size.Width);
            }

            // Rotation matrix around center
            Mat M = new Mat(2, 3, DepthType.Cv64F, 1);
            CvInvoke.GetRotationMatrix2D(rr.Center, angle, 1.0,M);

            // Rotate full image
            Mat rotated = new Mat();
            CvInvoke.WarpAffine(grayEq, rotated, M, grayEq.Size,
                Inter.Linear, Warp.Default, BorderType.Constant, new MCvScalar(0));

            // Crop the now-upright rectangle region
            int x = (int)Math.Round(rr.Center.X - size.Width / 2f) - pad;
            int y = (int)Math.Round(rr.Center.Y - size.Height / 2f) - pad;
            int w = (int)Math.Round(size.Width) + 2 * pad;
            int h = (int)Math.Round(size.Height) + 2 * pad;

            Rectangle roi = new Rectangle(x, y, w, h);
            roi = ClampRect(roi, rotated.Width, rotated.Height);
            if (roi.Width < 20 || roi.Height < 20)
            {
                rotated.Dispose();
                return new Mat();
            }

            Mat crop = new Mat(rotated, roi).Clone();
            rotated.Dispose();
            return crop;
        }

        // ===================== Utils =====================

        static IEnumerable<double> Scales(double start, double end, double step)
        {
            for (double s = start; s <= end + 1e-9; s += step)
                yield return s;
        }

        static Mat ResizeMat(Mat src, double scale)
        {
            Mat dst = new Mat();
            var newSize = new Size(
                Math.Max(1, (int)Math.Round(src.Width * scale)),
                Math.Max(1, (int)Math.Round(src.Height * scale)));
            CvInvoke.Resize(src, dst, newSize, 0, 0, Inter.Linear);
            return dst;
        }

        static Rectangle ClampRect(Rectangle r, int width, int height)
        {
            int x = Math.Max(0, r.X);
            int y = Math.Max(0, r.Y);
            int right = Math.Min(width, r.Right);
            int bottom = Math.Min(height, r.Bottom);
            int w = Math.Max(0, right - x);
            int h = Math.Max(0, bottom - y);
            return new Rectangle(x, y, w, h);
        }

        static void DrawRotatedRect(Mat bgr, RotatedRect rr, MCvScalar color, int thickness)
        {
            PointF[] pts = rr.GetVertices();
            Point[] p = Array.ConvertAll(pts, Point.Round);
            using VectorOfPoint vp = new VectorOfPoint(p);
            CvInvoke.Polylines(bgr, vp, true, color, thickness);
        }

        // Simple NMS using axis-aligned bounding boxes of rotated rects
        static List<DetectedLabel> NmsOnBoundingBoxes(List<DetectedLabel> input, double iouThreshold)
        {
            if (input.Count == 0) return new List<DetectedLabel>();

            var sorted = input.OrderByDescending(x => x.Score).ToList();
            var kept = new List<DetectedLabel>();

            foreach (var cand in sorted)
            {
                Rectangle a = cand.Rect.MinAreaRectToBoundingBox();
                bool overlaps = kept.Any(k =>
                {
                    Rectangle b = k.Rect.MinAreaRectToBoundingBox();
                    return IoU(a, b) > iouThreshold;
                });

                if (!overlaps)
                    kept.Add(cand);
            }
            return kept;
        }

        static double IoU(Rectangle a, Rectangle b)
        {
            int x1 = Math.Max(a.Left, b.Left);
            int y1 = Math.Max(a.Top, b.Top);
            int x2 = Math.Min(a.Right, b.Right);
            int y2 = Math.Min(a.Bottom, b.Bottom);

            int interW = Math.Max(0, x2 - x1);
            int interH = Math.Max(0, y2 - y1);
            double inter = interW * interH;
            double union = a.Width * a.Height + b.Width * b.Height - inter;
            return union <= 0 ? 0 : inter / union;
        }

        // ====== Records / Extensions ======
        record DetectedLabel(RotatedRect Rect, double Score);

    }

    static class RotatedRectExtensions
    {
        public static Rectangle MinAreaRectToBoundingBox(this RotatedRect rr)
        {
            PointF[] pts = rr.GetVertices();
            float minX = pts.Min(p => p.X);
            float minY = pts.Min(p => p.Y);
            float maxX = pts.Max(p => p.X);
            float maxY = pts.Max(p => p.Y);

            int x = (int)Math.Floor(minX);
            int y = (int)Math.Floor(minY);
            int w = (int)Math.Ceiling(maxX - minX);
            int h = (int)Math.Ceiling(maxY - minY);
            return new Rectangle(x, y, Math.Max(1, w), Math.Max(1, h));
        }
    }

}

