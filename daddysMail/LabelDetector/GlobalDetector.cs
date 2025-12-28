using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 using Emgu.CV;
namespace FindLabel
{
    /// <summary>
    /// מחלקה לתווית שנחתכה עם מידע
    /// Class for cropped label with information
    /// </summary>
    public class CroppedLabel
    {
        public int Id { get; set; }
        public Mat Image { get; set; }
        public PointF[] Corners { get; set; }
        public float RotationAngle { get; set; }
        public string FilePath { get; set; }
        public string DetectionMethod { get; set; }
    }
    public  class GlobalDetector
    {
       /// <summary>
       /// המר מלבן לפינות
       /// Convert rectangle to corners
       /// </summary>
        public static PointF[] RectangleToCorners(Rectangle rect)
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
        /// חשב זווית סיבוב של תווית
        /// Calculate rotation angle of label
        /// </summary>
        public static float CalculateRotationAngle(PointF[] corners)
        {
            if (corners.Length < 4)
                return 0;

            PointF[] ordered = OrderCorners(corners);

            // חשב זווית לפי הקו העליון
            // Calculate angle based on top edge
            float dx = ordered[1].X - ordered[0].X;
            float dy = ordered[1].Y - ordered[0].Y;

            float angle = (float)(Math.Atan2(dy, dx) * 180.0 / Math.PI);

            return angle;
        }
        /// <summary>
        /// סדר פינות (למעלה-שמאל, למעלה-ימין, למטה-ימין, למטה-שמאל)
        /// Order corners (top-left, top-right, bottom-right, bottom-left)
        /// </summary>
        public static PointF[] OrderCorners(PointF[] corners)
        {
            /*if (corners.Length != 4)
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

            return new PointF[] { topLeft, topRight, bottomRight, bottomLeft };*/
            if (corners.Length != 4)
                return corners;

            PointF[] pts = (PointF[])corners.Clone();

            // מיין לפי Y, ואז X
            Array.Sort(pts, (a, b) =>
            {
                int yComp = a.Y.CompareTo(b.Y);
                return yComp != 0 ? yComp : a.X.CompareTo(b.X);
            });

            // 2 עליונים
            PointF topLeft = pts[0].X < pts[1].X ? pts[0] : pts[1];
            PointF topRight = pts[0].X < pts[1].X ? pts[1] : pts[0];

            // 2 תחתונים
            PointF bottomLeft = pts[2].X < pts[3].X ? pts[2] : pts[3];
            PointF bottomRight = pts[2].X < pts[3].X ? pts[3] : pts[2];

            return new PointF[] { topLeft, topRight, bottomRight, bottomLeft };
        }

        /// <summary>
        /// חשב מרחק בין נקודות
        /// Calculate distance between points
        /// </summary>
        public static float Distance(PointF p1, PointF p2)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

       

    }
}
