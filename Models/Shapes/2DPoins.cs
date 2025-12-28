


namespace StickersDetector.Models.Shapes
{
    public class Point2D
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point2D() { }

        public Point2D(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X}, {Y})";
    }
}
