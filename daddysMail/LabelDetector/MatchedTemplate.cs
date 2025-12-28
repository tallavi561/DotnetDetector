using OpenCvSharp;
using System;
using System.Collections.Generic;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;
namespace FindLabel
{
    public class MatchedTemplate
    {

           public static string Main(string inputImagePath)
        {
            string location = Path.GetDirectoryName(inputImagePath) + "\\";
            string patternPath = location + "pattern.jpg";
            string outputPath = location + Path.GetFileNameWithoutExtension(inputImagePath) + "output_detected.jpg";

          

                Mat srcGray = Cv2.ImRead(inputImagePath, ImreadModes.Grayscale);
                Mat tplGray = Cv2.ImRead(patternPath, ImreadModes.Grayscale);

                if (srcGray.Empty() || tplGray.Empty())
                {
                    Console.WriteLine("Failed to load input.jpg or pattern.jpg");
                    return "";
                }

                // Improve contrast
                Cv2.EqualizeHist(srcGray, srcGray);
                Cv2.EqualizeHist(tplGray, tplGray);

                // Tuning knobs
                double threshold = 0.68;                  // try 0.62..0.75
                double minScale = 0.60, maxScale = 1.40, scaleStep = 0.10;
                double minAngle = -60, maxAngle = 60, angleStep = 6;   // diagonal support

                var detections = new List<Detection>();

                for (double angle = minAngle; angle <= maxAngle; angle += angleStep)
                {
                    for (double scale = minScale; scale <= maxScale; scale += scaleStep)
                    {
                        using Mat tplScaled = new Mat();
                       Cv2.Resize(tplGray, tplScaled, new Size(0, 0), scale, scale, InterpolationFlags.Area);

                    if (tplScaled.Width < 20 || tplScaled.Height < 20)
                            continue;

                        using Mat tplRot = RotateBound(tplScaled, angle);

                        if (tplRot.Width >= srcGray.Width || tplRot.Height >= srcGray.Height)
                            continue;

                        using Mat result = new Mat();
                        Cv2.MatchTemplate(srcGray, tplRot, result, TemplateMatchModes.CCoeffNormed);

                        while (true)
                        {
                            // FIX: no out _ in some OpenCvSharp versions
                            double minVal, maxVal;
                            Point minLoc, maxLoc;
                            Cv2.MinMaxLoc(result, out minVal, out maxVal, out minLoc, out maxLoc);

                            if (maxVal < threshold)
                                break;

                            var rect = new Rect(maxLoc, tplRot.Size());
                            detections.Add(new Detection(rect, maxVal, angle, scale));

                            // Suppress area in response map to avoid duplicates
                            Cv2.Rectangle(result, rect, Scalar.Black, -1);
                        }
                    }
                }

                var finalDetections = Nms(detections, iouThreshold: 0.35);

                Mat output = Cv2.ImRead(inputImagePath, ImreadModes.Color);
                foreach (var d in finalDetections)
                {
                    Cv2.Rectangle(output, d.Rect, Scalar.Red, 3);

                    // Optional: show score/angle
                    // Cv2.PutText(output, $"{d.Score:0.00} a={d.Angle:0}",
                    //     new Point(d.Rect.X, Math.Max(0, d.Rect.Y - 6)),
                    //     HersheyFonts.HersheySimplex, 0.6, Scalar.Yellow, 2);
                }

                Cv2.ImWrite(outputPath, output);

                Console.WriteLine($"Raw detections: {detections.Count}");
                Console.WriteLine($"Final detections: {finalDetections.Count}");
                Console.WriteLine($"Saved: {outputPath}");
               return outputPath;
            }

            // Rotate without cropping corners (expands canvas)
            static Mat RotateBound(Mat src, double angleDegrees)
            {
                double angle = angleDegrees * Math.PI / 180.0;
                double absCos = Math.Abs(Math.Cos(angle));
                double absSin = Math.Abs(Math.Sin(angle));

                int w = src.Width;
                int h = src.Height;

                int newW = (int)Math.Round(h * absSin + w * absCos);
                int newH = (int)Math.Round(h * absCos + w * absSin);

                Point2f center = new Point2f(w / 2f, h / 2f);
                Mat rotMat = Cv2.GetRotationMatrix2D(center, angleDegrees, 1.0);

                // Shift to new canvas center
                rotMat.Set(0, 2, rotMat.Get<double>(0, 2) + (newW / 2.0 - center.X));
                rotMat.Set(1, 2, rotMat.Get<double>(1, 2) + (newH / 2.0 - center.Y));

                Mat dst = new Mat();
                Cv2.WarpAffine(src, dst, rotMat, new Size(newW, newH),
                    InterpolationFlags.Linear, BorderTypes.Constant, Scalar.Black);

                rotMat.Dispose();
                return dst;
            }

            // Simple Non-Maximum Suppression to merge overlaps
            static List<Detection> Nms(List<Detection> dets, double iouThreshold)
            {
                dets.Sort((a, b) => b.Score.CompareTo(a.Score));
                var kept = new List<Detection>();

                foreach (var d in dets)
                {
                    bool overlaps = false;
                    foreach (var k in kept)
                    {
                        if (IoU(d.Rect, k.Rect) > iouThreshold)
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    if (!overlaps) kept.Add(d);
                }
                return kept;
            }

            static double IoU(Rect a, Rect b)
            {
                int x1 = Math.Max(a.Left, b.Left);
                int y1 = Math.Max(a.Top, b.Top);
                int x2 = Math.Min(a.Right, b.Right);
                int y2 = Math.Min(a.Bottom, b.Bottom);

                int interW = Math.Max(0, x2 - x1);
                int interH = Math.Max(0, y2 - y1);
                int interArea = interW * interH;

                int unionArea = a.Width * a.Height + b.Width * b.Height - interArea;
                if (unionArea <= 0) return 0.0;
                return (double)interArea / unionArea;
            }

            public record Detection(Rect Rect, double Score, double Angle, double Scale);
        
    }
}