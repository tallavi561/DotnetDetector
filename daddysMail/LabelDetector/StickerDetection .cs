using System.Drawing;

namespace FindLabel;

/// <summary>
/// מייצג מדבקה שזוהתה בתמונה
/// </summary>
public class StickerDetection
{
    /// <summary>
    /// ארבע הפינות של המדבקה בתמונה (בסדר: שמאל-עליון, ימין-עליון, ימין-תחתון, שמאל-תחתון)
    /// </summary>
    public PointF[] Corners { get; set; } = Array.Empty<PointF>();

    /// <summary>
    /// מספר ההתאמות הכולל שנמצאו בין התבנית לתמונה
    /// </summary>
    public int TotalMatches { get; set; }

    /// <summary>
    /// מספר ההתאמות שאומתו כנכונות (inliers) על ידי RANSAC
    /// </summary>
    public int Inliers { get; set; }

    /// <summary>
    /// רמת הביטחון בזיהוי (0-1), מחושב כיחס בין inliers לסך ההתאמות
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// נקודת המרכז של המדבקה
    /// </summary>
    public PointF Center
    {
        get
        {
            if (Corners.Length < 4)
                return PointF.Empty;

            float x = Corners.Average(p => p.X);
            float y = Corners.Average(p => p.Y);
            return new PointF(x, y);
        }
    }

    /// <summary>
    /// שטח המדבקה בפיקסלים (קירוב)
    /// </summary>
    public double Area
    {
        get
        {
            if (Corners.Length < 4)
                return 0;

            // חישוב שטח באמצעות נוסחת Shoelace
            double area = 0;
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                area += Corners[i].X * Corners[j].Y;
                area -= Corners[j].X * Corners[i].Y;
            }
            return Math.Abs(area / 2.0);
        }
    }

    public override string ToString()
    {
        return $"Detection: Confidence={Confidence:P0}, Matches={Inliers}/{TotalMatches}, Center={Center}";
    }
}