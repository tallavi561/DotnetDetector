using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindLabel
{
    public class EmguCVSample
    {

        public class Detection
        {
            public Rectangle Rect;
            public RotatedRect RotRect;
            public double Score; // ציון פנימי לסינון/דיבוג
        }

        private  static List<Detection> DetectLabels(Mat gray)
        {
            if (gray.NumberOfChannels != 1)
                throw new ArgumentException("Input must be grayscale (1 channel).");

            // 1) ניקוי רעש עדין
            Mat blur = new Mat();
            CvInvoke.GaussianBlur(gray, blur, new Size(5, 5), 0);

            // 2) בינריזציה אדפטיבית כדי לתפוס “נייר לבן” גם בתאורה לא אחידה
            //    THRESH_BINARY => לבן נשאר לבן, רקע כהה נשאר כהה
            Mat bin = new Mat();
            CvInvoke.AdaptiveThreshold(
                blur, bin, 255,
                AdaptiveThresholdType.GaussianC,
                ThresholdType.Binary,
                25,   // blockSize (אי-זוגי). אם המדבקה קטנה יותר, הקטן ל-31/25
                -8    // C: שלילי עוזר להוציא יותר “לבן”
            );

            // 3) מורפולוגיה: סגירה כדי לחבר קצוות, לבטל קרעים
            Mat closed = new Mat();
            Mat k = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, new Size(9, 9), new Point(-1, -1));
            CvInvoke.MorphologyEx(bin, closed, MorphOp.Close, k, new Point(-1, -1), 2, BorderType.Reflect, default);

            // 4) מציאת קונטורים
            using var contours = new VectorOfVectorOfPoint();
            Mat hier = new Mat();
            CvInvoke.FindContours(closed, contours, hier, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            var results = new List<Detection>();

            int imgArea = gray.Rows * gray.Cols;
            double minArea = imgArea * 0.003;  // 0.3% מהתמונה (כוון לפי גודל מדבקות)
            double maxArea = imgArea * 0.45;   // לא לקחת כמעט כל התמונה

            for (int i = 0; i < contours.Size; i++)
            {
                using var c = contours[i];

                double area = CvInvoke.ContourArea(c);
                if (area < minArea || area > maxArea)
                    continue;

                Rectangle rect = CvInvoke.BoundingRectangle(c);
                if (rect.Width < 40 || rect.Height < 25)
                    continue;

                double ar = rect.Width / (double)rect.Height;
                if (ar < 0.6 || ar > 6.0) // מדבקות יכולות להיות “מאורכות” אבל לא קיצוני
                    continue;

                // כמה זה "מלבני": שטח קונטור / שטח מלבן תחום
                double rectangularity = area / (rect.Width * (double)rect.Height);
                if (rectangularity < 0.55) // אם נמוך מדי זה לרוב שקית/קמט/ברק
                    continue;

                // 5) בדיקת “טקסט שחור” בתוך המועמד:
                //    במדבקה אמורה להיות כמות מסוימת של פיקסלים כהים (טקסט/ברקוד).
                double darkRatio = EstimateDarkTextRatio(gray, rect);
                if (darkRatio < 0.010) // 1% כהה לפחות (כוון לפי איכות/חשיפה)
                    continue;

                // 6) RotatedRect (אופציונלי לשיפור): מינימום מלבן מסובב
                RotatedRect rrect = CvInvoke.MinAreaRect(c);

                // ציון פשוט
                double score = rectangularity * 0.7 + Math.Min(darkRatio * 20.0, 1.0) * 0.3;

                results.Add(new Detection
                {
                    Rect = rect,
                    RotRect = rrect,
                    Score = score
                });
            }

            // 7) NMS פשוט: מסירים חפיפות גדולות (כדי לא לקבל כפילויות)
            results = NonMaxSuppression(results, 0.4);

            return results;
        }

        private static double EstimateDarkTextRatio(Mat gray, Rectangle rect)
        {
            rect = ClampRect(rect, gray.Width, gray.Height);

            using Mat roi = new Mat(gray, rect);

            // שיפור קונטרסט מקומי
            using Mat eq = new Mat();
            CvInvoke.EqualizeHist(roi, eq);

            // מחפשים פיקסלים כהים (טקסט שחור) – סף קבוע עובד טוב אחרי EqualizeHist
            using Mat dark = new Mat();
            CvInvoke.Threshold(eq, dark, 90, 255, ThresholdType.BinaryInv); // כהה=>לבן במסכה

            int darkCount = CvInvoke.CountNonZero(dark);
            int total = rect.Width * rect.Height;

            return total > 0 ? darkCount / (double)total : 0.0;
        }

        private static Rectangle ClampRect(Rectangle r, int w, int h)
        {
            int x = Math.Max(0, r.X);
            int y = Math.Max(0, r.Y);
            int right = Math.Min(w, r.Right);
            int bottom = Math.Min(h, r.Bottom);
            int ww = Math.Max(1, right - x);
            int hh = Math.Max(1, bottom - y);
            return new Rectangle(x, y, ww, hh);
        }

        private static List<Detection> NonMaxSuppression(List<Detection> dets, double iouThresh)
        {
            dets.Sort((a, b) => b.Score.CompareTo(a.Score));
            var kept = new List<Detection>();

            foreach (var d in dets)
            {
                bool overlap = false;
                foreach (var k in kept)
                {
                    if (IoU(d.Rect, k.Rect) > iouThresh)
                    {
                        overlap = true;
                        break;
                    }
                }
                if (!overlap) kept.Add(d);
            }
            return kept;
        }

        private static double IoU(Rectangle a, Rectangle b)
        {
            int x1 = Math.Max(a.Left, b.Left);
            int y1 = Math.Max(a.Top, b.Top);
            int x2 = Math.Min(a.Right, b.Right);
            int y2 = Math.Min(a.Bottom, b.Bottom);

            int iw = Math.Max(0, x2 - x1);
            int ih = Math.Max(0, y2 - y1);
            double inter = iw * (double)ih;
            double union = a.Width * (double)a.Height + b.Width * (double)b.Height - inter;
            return union <= 0 ? 0 : inter / union;
        }

        public static void Main(string inputPath )
        {

            // Directory.CreateDirectory(outDir);
            string outDir = Path.GetDirectoryName(inputPath);
            using Mat gray = CvInvoke.Imread(inputPath, ImreadModes.Grayscale);

            var dets = DetectLabels(gray);

            // ציור תוצאות
            using Mat color = new Mat();
            CvInvoke.CvtColor(gray, color, ColorConversion.Gray2Bgr);

            int idx = 0;
            foreach (var d in dets)
            {
                CvInvoke.Rectangle(color, d.Rect, new MCvScalar(255, 255, 0), 3);

                // שמירת CROP (מלבן ישר)
                using Mat crop = new Mat(gray, d.Rect);
                string cropPath = Path.Combine(outDir, $"label_{idx:000}.png");
                crop.Save(cropPath);

                idx++;
            }

            string markedPath = Path.Combine(outDir, "marked.png");
            color.Save(markedPath);

            Console.WriteLine($"Done. Found {dets.Count} labels");
            Console.WriteLine($"Marked image: {markedPath}");
        }
    
    }
}
 




