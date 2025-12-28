using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
//using OpenCvSharp;
using System.Drawing;
using System.Drawing.Imaging;

public class LabelDetector
{
    private readonly InferenceSession session;
    private const int ImgSize = 640;
    private const int MIN_LABEL_WIDTH = 500;
    private const int MIN_LABEL_HEIGHT = 800;
    private const int MAX_LABEL_WIDTH = 3000;
    private const int MAX_LABEL_HEIGHT = 5000;
    public LabelDetector(string modelPath)
    {
        session = new InferenceSession(modelPath);
    }

   /* private float[] Preprocess(Mat img)
    {
        Mat resized = img.Resize(new OpenCvSharp.Size(ImgSize, ImgSize));
        float[] input = new float[1 * 3 * ImgSize * ImgSize];
        int index = 0;

        for (int y = 0; y < ImgSize; y++)
        {
            for (int x = 0; x < ImgSize; x++)
            {
                Vec3b pixel = resized.At<Vec3b>(y, x);
                input[index++] = pixel[2] / 255f; // R
                input[index++] = pixel[1] / 255f; // G
                input[index++] = pixel[0] / 255f; // B
            }
        }
        return input;
    }

    public Rect? DetectLabel(Mat img)
    {
        float[] input = Preprocess(img);

        var tensor = new DenseTensor<float>(input, new[] { 1, 3, ImgSize, ImgSize });

        var inputs = new List<NamedOnnxValue>()
        {
            NamedOnnxValue.CreateFromTensor("images", tensor)
        };

        using var results = session.Run(inputs);

        float[] output = results.First().AsEnumerable<float>().ToArray();
        int stride = 85; // YOLO format: x,y,w,h,conf + classes

        float bestConf = 0f;
        Rect? bestBox = null;

        for (int i = 0; i < output.Length; i += stride)
        {
            float conf = output[i + 4];
            if (conf < 0.40f) continue; // confidence threshold

            // class ID (assume only 1 class: "label")
            float clsConf = output.Skip(i + 5).Take(1).First();
            float score = conf * clsConf;

            if (score > bestConf)
            {
                float x = output[i] * img.Width;
                float y = output[i + 1] * img.Height;
                float w = output[i + 2] * img.Width;
                float h = output[i + 3] * img.Height;

                int left = (int)(x - w / 2);
                int top = (int)(y - h / 2);

                bestBox = new Rect(left, top, (int)w, (int)h);
                bestConf = score;
            }
        }

        return bestBox;
    }

    private static string GetDefaultOutputPath(string inputPath)
    {
        string directory = Path.GetDirectoryName(inputPath) ?? ".";
        string filename = Path.GetFileNameWithoutExtension(inputPath);
        string extension = Path.GetExtension(inputPath);
        return Path.Combine(directory, $"{filename}_signed{extension}");
    }*/

    public static void ProcessImage(string inputPath, string outputPath, string signatureText)
    {
        using (Bitmap originalImage = new Bitmap(inputPath))
        {
            // Convert to grayscale if needed
            Bitmap grayImage = ConvertToGrayscale(originalImage);

            // Detect labels (white rectangles/regions on the packages)
            List<Rectangle> labels = DetectLabels(grayImage);

            Console.WriteLine($"Detected {labels.Count} potential labels");

            // Create output image
            using (Bitmap outputImage = new Bitmap(originalImage))
            using (Graphics g = Graphics.FromImage(outputImage))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                // Sign each detected label
                foreach (var label in labels)
                {
                    SignLabel(g, label, signatureText);
                }

                // Save the output
                outputImage.Save(outputPath, ImageFormat.Jpeg);
            }

            grayImage.Dispose();
        }
    }

    private static Bitmap ConvertToGrayscale(Bitmap original)
    {
        Bitmap grayscale = new Bitmap(original.Width, original.Height);

        using (Graphics g = Graphics.FromImage(grayscale))
        {
            // Create grayscale color matrix
            ColorMatrix colorMatrix = new ColorMatrix(
                new float[][]
                {
                        new float[] {.299f, .299f, .299f, 0, 0},
                        new float[] {.587f, .587f, .587f, 0, 0},
                        new float[] {.114f, .114f, .114f, 0, 0},
                        new float[] {0, 0, 0, 1, 0},
                        new float[] {0, 0, 0, 0, 1}
                });

            using (ImageAttributes attributes = new ImageAttributes())
            {
                attributes.SetColorMatrix(colorMatrix);
                g.DrawImage(original,
                    new Rectangle(0, 0, original.Width, original.Height),
                    0, 0, original.Width, original.Height,
                    GraphicsUnit.Pixel, attributes);
            }
        }

        return grayscale;
    }

    private static List<Rectangle> DetectLabels(Bitmap image)
    {
        List<Rectangle> labels = new List<Rectangle>();
        int width = image.Width;
        int height = image.Height;

        // Create binary threshold image
        bool[,] binaryImage = new bool[width, height];
        int threshold = 200; // Threshold for detecting white regions

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = image.GetPixel(x, y);
                int brightness = (pixel.R + pixel.G + pixel.B) / 3;
                binaryImage[x, y] = brightness > threshold;
            }
        }

        // Find connected components (white regions)
        bool[,] visited = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (binaryImage[x, y] && !visited[x, y])
                {
                    Rectangle boundingBox = FloodFillAndGetBounds(
                        binaryImage, visited, x, y, width, height);

                    // Filter by size to identify likely labels
                    if (IsLikelyLabel(boundingBox))
                    {
                        labels.Add(boundingBox);
                    }
                }
            }
        }

        // Merge overlapping or nearby labels
        labels = MergeNearbyLabels(labels);

        return labels;
    }

    private static Rectangle FloodFillAndGetBounds(
        bool[,] binaryImage, bool[,] visited, int startX, int startY,
        int width, int height)
    {
        int minX = startX, maxX = startX;
        int minY = startY, maxY = startY;

        Queue<Point> queue = new Queue<Point>();
        queue.Enqueue(new Point(startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            Point p = queue.Dequeue();

            minX = Math.Min(minX, p.X);
            maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y);
            maxY = Math.Max(maxY, p.Y);

            // Check 4-connected neighbors
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nx = p.X + dx[i];
                int ny = p.Y + dy[i];

                if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
                    binaryImage[nx, ny] && !visited[nx, ny])
                {
                    visited[nx, ny] = true;
                    queue.Enqueue(new Point(nx, ny));
                }
            }
        }

        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static bool IsLikelyLabel(Rectangle rect)
    {
        return rect.Width >= MIN_LABEL_WIDTH &&
               rect.Height >= MIN_LABEL_HEIGHT &&
               rect.Width <= MAX_LABEL_WIDTH &&
               rect.Height <= MAX_LABEL_HEIGHT &&
               rect.Width * rect.Height > 10000; // Minimum area
    }

    private static List<Rectangle> MergeNearbyLabels(List<Rectangle> labels)
    {
        if (labels.Count <= 1) return labels;

        List<Rectangle> merged = new List<Rectangle>();
        bool[] used = new bool[labels.Count];

        for (int i = 0; i < labels.Count; i++)
        {
            if (used[i]) continue;

            Rectangle current = labels[i];

            for (int j = i + 1; j < labels.Count; j++)
            {
                if (used[j]) continue;

                // Check if labels are close enough to merge
                Rectangle other = labels[j];
                Rectangle expanded = current;
                expanded.Inflate(50, 50); // Merge if within 50 pixels

                if (expanded.IntersectsWith(other))
                {
                    current = Rectangle.Union(current, other);
                    used[j] = true;
                }
            }

            merged.Add(current);
            used[i] = true;
        }

        return merged;
    }

    private static void SignLabel(Graphics g, Rectangle label, string signatureText)
    {
        // Draw a red border around the label
        using (Pen borderPen = new Pen(Color.Red, 3))
        {
            g.DrawRectangle(borderPen, label);
        }

        // Calculate signature position (bottom of the label)
        int fontSize = Math.Max(10, Math.Min(20, label.Height / 6));
        using (Font font = new Font("Arial", fontSize, FontStyle.Bold))
        {
            SizeF textSize = g.MeasureString(signatureText, font);

            // Position at bottom center of label
            float x = label.X + (label.Width - textSize.Width) / 2;
            float y = label.Y + label.Height - textSize.Height - 5;

            // Draw background rectangle for better visibility
            RectangleF textRect = new RectangleF(x - 5, y - 2,
                textSize.Width + 10, textSize.Height + 4);

            using (Brush bgBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            {
                g.FillRectangle(bgBrush, textRect);
            }

            // Draw the signature text
            using (Brush textBrush = new SolidBrush(Color.Red))
            {
                g.DrawString(signatureText, font, textBrush, x, y);
            }

            // Draw a small checkmark or stamp icon
            DrawStampIcon(g, label);
        }
    }

    private static void DrawStampIcon(Graphics g, Rectangle label)
    {
        // Draw a checkmark in the top-right corner of the label
        int size = Math.Min(30, label.Height / 4);
        int x = label.Right - size - 10;
        int y = label.Top + 10;

        using (Pen checkPen = new Pen(Color.Green, 3))
        {
            checkPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            checkPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            // Draw checkmark
            Point[] checkmark = new Point[]
            {
                    new Point(x, y + size / 2),
                    new Point(x + size / 3, y + size),
                    new Point(x + size, y)
            };

            g.DrawLines(checkPen, checkmark);
        }

        // Draw circle around checkmark
        using (Pen circlePen = new Pen(Color.Green, 2))
        {
            g.DrawEllipse(circlePen, x - 5, y - 5, size + 10, size + 10);
        }
    }
}
