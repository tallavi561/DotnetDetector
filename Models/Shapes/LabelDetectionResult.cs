using StickersDetector.Models.Shapes;
public sealed class LabelDetectionResult
{
    public string LabelName { get; }
    public IReadOnlyList<Point2D> Corners { get; }
    public double Confidence { get; }
    public int Inliers { get; }
    public int Matches { get; }

    // בנאי שמתאים לקריאה ב-LabelDetector
    public LabelDetectionResult(
        string labelName, 
        IReadOnlyList<Point2D> corners, 
        double confidence, 
        int inliers, 
        int matches)
    {
        LabelName = labelName;
        Corners = corners;
        Confidence = confidence;
        Inliers = inliers;
        Matches = matches;
    }

    // מאפיינים מחושבים (דוגמה למימוש בסיסי)
    public Point2D Center => new Point2D(
        (int)Corners.Average(p => p.X), 
        (int)Corners.Average(p => p.Y));

    public bool IsReliable => Confidence >= 0.6;
}