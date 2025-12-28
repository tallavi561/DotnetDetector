using StickersDetector.Models.Shapes;

namespace DotnetDetector.Models.Shapes
{
    /// <summary>
    /// Represents a 2D rectangle defined by two points,
    /// width, height and rotation (in degrees).
    /// All values are integers.
    /// </summary>
    public class Rectangle2D
    {
        /// <summary>
        /// First corner point (e.g. top-left or any reference point)
        /// </summary>
        public Point2D P1 { get; set; }

        /// <summary>
        /// Second corner point (opposite to P1)
        /// </summary>
        public Point2D P2 { get; set; }

        /// <summary>
        /// Rectangle width (pixels)
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Rectangle height (pixels)
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Rotation angle in degrees.
        /// Convention: clockwise, 0 = axis-aligned
        /// </summary>
        public int RotationDeg { get; set; }

        public Rectangle2D(
            Point2D p1,
            Point2D p2,
            int width,
            int height,
            int rotationDeg)
        {
            P1 = p1;
            P2 = p2;
            Width = width;
            Height = height;
            RotationDeg = rotationDeg;
        }

        /// <summary>
        /// Center point of the rectangle (integer precision)
        /// </summary>
        public Point2D Center =>
            new Point2D(
                (P1.X + P2.X) / 2,
                (P1.Y + P2.Y) / 2
            );

        public override string ToString()
        {
            return $"Rect [P1={P1}, P2={P2}, W={Width}, H={Height}, Rot={RotationDeg}°]";
        }
    }
}
