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
    public  class ChatGpt2
    {



        public static string Main(string inputPath  )
        {
            
            string outputPath = Path.GetDirectoryName(inputPath) +  "\\labels_rotated_red.jpg";

            // Load grayscale image
            Mat gray = CvInvoke.Imread(inputPath, ImreadModes.Grayscale);
            Mat color = new Mat();
            CvInvoke.CvtColor(gray, color, ColorConversion.Gray2Bgr);

            // Threshold - white labels
            Mat thresh = new Mat();
            CvInvoke.Threshold(gray, thresh, 180, 255, ThresholdType.Binary);

            // Morphology Close
            Mat kernel = CvInvoke.GetStructuringElement(
                MorphShapes.Rectangle,
                new Size(200, 200),
                new Point(-1, -1));

            CvInvoke.MorphologyEx(
                thresh,
                thresh,
                MorphOp.Close,
                kernel,
                new Point(-1, -1),
                1,
                BorderType.Default,
                new MCvScalar());

            // Find contours
            using VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(
                thresh,
                contours,
                null,
                RetrType.External,
                ChainApproxMethod.ChainApproxSimple);

            // Loop contours
            for (int i = 0; i < contours.Size; i++)
            {
                double area = CvInvoke.ContourArea(contours[i]);
                if (area < 5000)
                    continue;

                // Rotated rectangle
                RotatedRect rect = CvInvoke.MinAreaRect(contours[i]);
                PointF[] pts = rect.GetVertices();
                Point[] points = Array.ConvertAll(pts, p => Point.Round(p));

                // Draw rotated rectangle
                using VectorOfPoint vp = new VectorOfPoint(points);
                CvInvoke.Polylines(
                    color,
                    vp,
                    true,
                    new MCvScalar(0, 0, 255), // Red
                    3);
            }

            // Save output
            CvInvoke.Imwrite(outputPath, color);

            Console.WriteLine("Done. Saved to " + outputPath);
            return outputPath;
        }
    }

}

