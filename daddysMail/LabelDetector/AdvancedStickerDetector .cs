using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.Util;


namespace FindLabel;

/// <summary>
/// דטקטור מתקדם לזיהוי מדבקות לפי מבנה ויזואלי
/// תומך בזיהוי מדבקות מסובבות ובזוויות שונות באמצעות Feature Matching
/// </summary>
public class AdvancedStickerDetector : IDisposable
{
    private readonly Mat _template;
    private readonly Mat _templateGray;
    private readonly SIFT _sift;
    private readonly VectorOfKeyPoint _templateKeypoints;
    private readonly Mat _templateDescriptors;
    private readonly FlannBasedMatcher _matcher;
    private bool _disposed;

    /// <summary>
    /// אתחול הדטקטור עם תמונת תבנית
    /// </summary>
    /// <param name="templatePath">נתיב לתמונת התבנית של המדבקה</param>
    public AdvancedStickerDetector(string templatePath, Mat template)
    {
        _templateGray = template;
        /*if (!File.Exists(templatePath))
            throw new FileNotFoundException($"קובץ התבנית לא נמצא: {templatePath}");

        // קריאת תמונת התבנית
        _template = CvInvoke.Imread(templatePath, ImreadModes.Grayscale);
        if (template.IsEmpty)
            throw new InvalidOperationException($"לא ניתן לקרוא את תמונת התבנית: {templatePath}");

        // המרה לגווני אפור
        _templateGray = new Mat();
        CvInvoke.CvtColor(_template, _templateGray, ColorConversion.Bgr2Gray);

        Console.WriteLine($"תמונת תבנית נטענה: {_template.Width}x{_template.Height}");
        */
        // אתחול SIFT detector
        _sift = new SIFT();

        // חישוב keypoints ו-descriptors של התבנית
        _templateKeypoints = new VectorOfKeyPoint();
        _templateDescriptors = new Mat();
        _sift.DetectAndCompute(template, null, _templateKeypoints, _templateDescriptors, false);

        if (_templateKeypoints.Size == 0)
            throw new InvalidOperationException("לא נמצאו נקודות מפתח בתמונת התבנית. נסה תמונה עם יותר פרטים.");

        Console.WriteLine($"נמצאו {_templateKeypoints.Size} נקודות מפתח בתבנית");

        // אתחול FLANN matcher עם פרמטרים מתאימים ל-SIFT
        var indexParams = new Emgu.CV.Flann.KdTreeIndexParams(5);
        var searchParams = new Emgu.CV.Flann.SearchParams(50);
        _matcher = new FlannBasedMatcher(indexParams, searchParams);
    }

    /// <summary>
    /// מחפש מדבקות בתמונה
    /// </summary>
    /// <param name="image">התמונה לחיפוש</param>
    /// <param name="minMatches">מספר מינימלי של התאמות נדרשות לזיהוי</param>
    /// <param name="ratioThreshold">סף ל-Lowe's ratio test (ברירת מחדל: 0.7)</param>
    /// <param name="ransacThreshold">סף למרחק ב-RANSAC (ברירת מחדל: 5.0)</param>
    /// <returns>רשימה של מדבקות שזוהו</returns>
    /*public List<StickerDetection> FindStickers(
        Mat image,
        int minMatches = 15,
        double ratioThreshold = 0.7,
        double ransacThreshold = 5.0)
    {
        if (image.IsEmpty)
            throw new ArgumentException("התמונה ריקה", nameof(image));

        var results = new List<StickerDetection>();

        // המרה לגווני אפור
        //using var gray = new Mat();
       // CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);

        // זיהוי features בתמונה
        using var keypoints = new VectorOfKeyPoint();
        using var descriptors = new Mat();
        _sift.DetectAndCompute(image, null, keypoints, descriptors, false);

        if (descriptors.IsEmpty || keypoints.Size < minMatches)
        {
            Console.WriteLine($"  נמצאו רק {keypoints.Size} נקודות מפתח - לא מספיק לזיהוי");
            return results;
        }

        Console.WriteLine($"  נמצאו {keypoints.Size} נקודות מפתח בתמונה");

        // התאמת features
        using var matches = new VectorOfVectorOfDMatch();
        _matcher.KnnMatch(_templateDescriptors, descriptors, matches, 2);

        // סינון התאמות טובות - Lowe's ratio test
        var goodMatches = new List<MDMatch>();
        for (int i = 0; i < matches.Size; i++)
        {
            if (matches[i].Size >= 2)
            {
                var m = matches[i][0];
                var n = matches[i][1];
                if (m.Distance < ratioThreshold * n.Distance)
                {
                    goodMatches.Add(m);
                }
            }
        }

        Console.WriteLine($"  נמצאו {goodMatches.Count} התאמות טובות");

        if (goodMatches.Count < minMatches)
            return results;

        // חילוץ נקודות מההתאמות
        var srcPoints = new PointF[goodMatches.Count];
        var dstPoints = new PointF[goodMatches.Count];

        for (int i = 0; i < goodMatches.Count; i++)
        {
            srcPoints[i] = _templateKeypoints[goodMatches[i].QueryIdx].Point;
            dstPoints[i] = keypoints[goodMatches[i].TrainIdx].Point;
        }

        // חישוב homography matrix
        try
        {
            using var srcMat = new VectorOfPointF(srcPoints);
            using var dstMat = new VectorOfPointF(dstPoints);
            using var mask = new Mat();

            var homography = CvInvoke.FindHomography(
                srcMat,
                dstMat,
                RobustEstimationAlgorithm.Ransac,
                ransacThreshold,
                mask);

            if (homography == null || homography.IsEmpty)
            {
                Console.WriteLine("  לא ניתן לחשב homography");
                return results;
            }

            // חישוב פינות המדבקה בתמונה
            var corners = new PointF[]
            {
                new PointF(0, 0),
                new PointF(_templateGray.Width, 0),
                new PointF(_templateGray.Width, _templateGray.Height),
                new PointF(0, _templateGray.Height)
            };

            using var cornersVec = new VectorOfPointF(corners);
            using var transformedCornersVec = new VectorOfPointF();

            CvInvoke.PerspectiveTransform(cornersVec, transformedCornersVec, homography);
            var transformedCorners = transformedCornersVec.ToArray();

            // ספירת inliers
            int inliers = 0;
            byte[] maskArray = new byte[mask.Rows];
            mask.CopyTo(maskArray);
            inliers = maskArray.Count(b => b != 0);

            double confidence = (double)inliers / goodMatches.Count;

            // בדיקת תקינות הזיהוי - הפינות צריכות להיות בתוך התמונה
            bool validDetection = transformedCorners.All(p =>
                p.X >= 0 && p.X < image.Width &&
                p.Y >= 0 && p.Y < image.Height);

            if (!validDetection)
            {
                Console.WriteLine("  הזיהוי מחוץ לגבולות התמונה - נדחה");
                return results;
            }

            results.Add(new StickerDetection
            {
                Corners = transformedCorners,
                TotalMatches = goodMatches.Count,
                Inliers = inliers,
                Confidence = confidence
            });

            homography.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  שגיאה בחישוב homography: {ex.Message}");
        }

        return results;
    }
    public List<StickerDetection> FindStickers(
                                                Mat image,
                                                int minMatches = 15,
                                                double ratioThreshold = 0.7,
                                                double ransacThreshold = 5.0)
    {
        if (image == null || image.IsEmpty)
            throw new ArgumentException("התמונה ריקה", nameof(image));

        var results = new List<StickerDetection>();

        // --------------------------------------------------
        // 1) הקטנת תמונה (שיפור ביצועים + יציבות)
        // --------------------------------------------------
        const double downscale = 0.5;

        using var workImage = new Mat();
        CvInvoke.Resize(image, workImage, new Size(0, 0),
            downscale, downscale, Inter.Area);

        // --------------------------------------------------
        // 2) Detect + Compute
        // --------------------------------------------------
        using var keypoints = new VectorOfKeyPoint();
        using var descriptors = new Mat();

        _sift.DetectAndCompute(workImage, null, keypoints, descriptors, false);

        if (descriptors.IsEmpty || keypoints.Size < minMatches)
            return results;

        // --------------------------------------------------
        // 3) KNN Match (עם Dispose נכון!)
        // --------------------------------------------------
        using var knnMatches = new VectorOfVectorOfDMatch();

        _matcher.KnnMatch(
            _templateDescriptors,
            descriptors,
            knnMatches,
            k: 2,
            mask: null);

        var goodMatches = new List<MDMatch>(knnMatches.Size);

        for (int i = 0; i < knnMatches.Size; i++)
        {
            using VectorOfDMatch v = knnMatches[i];   // 🔴 קריטי

            if (v.Size < 2)
                continue;

            MDMatch m = v[0];
            MDMatch n = v[1];

            if (m.Distance < ratioThreshold * n.Distance)
                goodMatches.Add(m);
        }

        knnMatches.Clear(); // 🔴 משחרר זיכרון כבד מוקדם

        if (goodMatches.Count < minMatches)
            return results;

        // --------------------------------------------------
        // 4) חילוץ נקודות להתאמה (עם החזרת Scale)
        // --------------------------------------------------
        var srcPoints = new PointF[goodMatches.Count];
        var dstPoints = new PointF[goodMatches.Count];

        for (int i = 0; i < goodMatches.Count; i++)
        {
            srcPoints[i] =
                _templateKeypoints[goodMatches[i].QueryIdx].Point;

            var p = keypoints[goodMatches[i].TrainIdx].Point;
            dstPoints[i] = new PointF(
                (float)(p.X / downscale),
                (float)(p.Y / downscale));
        }

        // --------------------------------------------------
        // 5) Homography (RANSAC)
        // --------------------------------------------------
        using var srcVec = new VectorOfPointF(srcPoints);
        using var dstVec = new VectorOfPointF(dstPoints);
        using var inlierMask = new Mat();

        Mat homography = CvInvoke.FindHomography(
            srcVec,
            dstVec,
            RobustEstimationAlgorithm.Ransac,
            ransacThreshold,
            inlierMask);

        if (homography == null || homography.IsEmpty)
            return results;

        // --------------------------------------------------
        // 6) Transform corners
        // --------------------------------------------------
        var tplCorners = new[]
        {
        new PointF(0, 0),
        new PointF(_templateGray.Width, 0),
        new PointF(_templateGray.Width, _templateGray.Height),
        new PointF(0, _templateGray.Height)
    };

        using var tplCornersVec = new VectorOfPointF(tplCorners);
        using var imgCornersVec = new VectorOfPointF();

        CvInvoke.PerspectiveTransform(
            tplCornersVec, imgCornersVec, homography);

        var imgCorners = imgCornersVec.ToArray();

        // --------------------------------------------------
        // 7) Confidence + Validation
        // --------------------------------------------------
        byte[] maskBytes = new byte[inlierMask.Rows];
        inlierMask.CopyTo(maskBytes);

        int inliers = maskBytes.Count(b => b != 0);
        double confidence = (double)inliers / goodMatches.Count;

        bool insideImage = imgCorners.All(p =>
            p.X >= 0 && p.X < image.Width &&
            p.Y >= 0 && p.Y < image.Height);

        if (!insideImage)
            return results;

        // --------------------------------------------------
        // 8) Result
        // --------------------------------------------------
        results.Add(new StickerDetection
        {
            Corners = imgCorners,
            TotalMatches = goodMatches.Count,
            Inliers = inliers,
            Confidence = confidence
        });

        homography.Dispose();
        return results;
    }*/
    public List<StickerDetection> FindStickers(
    Mat image,
    int minMatches = 15,
    double ratioThreshold = 0.7,
    double ransacThreshold = 5.0)
    {
        if (image == null || image.IsEmpty)
            throw new ArgumentException("Image is empty");

        var results = new List<StickerDetection>();

        const double downscale = 0.5;

        using var workImage = new Mat();
        CvInvoke.Resize(image, workImage, new Size(0, 0),
            downscale, downscale, Inter.Area);

        using var keypoints = new VectorOfKeyPoint();
        using var descriptors = new Mat();

        _sift.DetectAndCompute(workImage, null, keypoints, descriptors, false);

        if (descriptors.IsEmpty || keypoints.Size < minMatches)
            return results;

        // 🔴 Matcher מקומי בלבד!
        using var matcher = new BFMatcher(DistanceType.L2, crossCheck: false);
        using var knnMatches = new VectorOfVectorOfDMatch();

        matcher.KnnMatch(
            _templateDescriptors,
            descriptors,
            knnMatches,
            k: 2,
            mask: null);

        var goodMatches = new List<MDMatch>(knnMatches.Size);

        for (int i = 0; i < knnMatches.Size; i++)
        {
            using VectorOfDMatch v = knnMatches[i];

            if (v.Size < 2)
                continue;

            MDMatch m = v[0];
            MDMatch n = v[1];

            if (m.Distance < ratioThreshold * n.Distance)
                goodMatches.Add(m);
        }

        knnMatches.Clear();

        if (goodMatches.Count < minMatches)
            return results;

        var srcPoints = new PointF[goodMatches.Count];
        var dstPoints = new PointF[goodMatches.Count];

        for (int i = 0; i < goodMatches.Count; i++)
        {
            srcPoints[i] =
                _templateKeypoints[goodMatches[i].QueryIdx].Point;

            var p = keypoints[goodMatches[i].TrainIdx].Point;
            dstPoints[i] = new PointF(
                (float)(p.X / downscale),
                (float)(p.Y / downscale));
        }

        using var srcVec = new VectorOfPointF(srcPoints);
        using var dstVec = new VectorOfPointF(dstPoints);
        using var inlierMask = new Mat();

        using Mat homography = CvInvoke.FindHomography(
            srcVec,
            dstVec,
            RobustEstimationAlgorithm.Ransac,
            ransacThreshold,
            inlierMask);

        if (homography == null || homography.IsEmpty)
            return results;

        var tplCorners = new[]
        {
        new PointF(0, 0),
        new PointF(_templateGray.Width, 0),
        new PointF(_templateGray.Width, _templateGray.Height),
        new PointF(0, _templateGray.Height)
    };

        using var tplVec = new VectorOfPointF(tplCorners);
        using var imgVec = new VectorOfPointF();

        CvInvoke.PerspectiveTransform(tplVec, imgVec, homography);
        var imgCorners = imgVec.ToArray();

        if (!imgCorners.All(p =>
            p.X >= 0 && p.X < image.Width &&
            p.Y >= 0 && p.Y < image.Height))
            return results;

        byte[] maskBytes = new byte[inlierMask.Rows];
        inlierMask.CopyTo(maskBytes);

        int inliers = maskBytes.Count(b => b != 0);

        results.Add(new StickerDetection
        {
            Corners = imgCorners,
            TotalMatches = goodMatches.Count,
            Inliers = inliers,
            Confidence = (double)inliers / goodMatches.Count
        });

        return results;
    }



    /// <summary>
    /// מסמן את המדבקות שנמצאו על התמונה
    /// </summary>
    /// <param name="image">התמונה המקורית</param>
    /// <param name="detections">רשימת המדבקות שזוהו</param>
    /// <param name="color">צבע הסימון (ברירת מחדל: ירוק)</param>
    /// <param name="thickness">עובי הקו (ברירת מחדל: 5)</param>
    /// <returns>תמונה עם סימוני הזיהויים</returns>
    public Mat DrawDetections(
        Mat image,
        List<StickerDetection> detections,
        MCvScalar? color = null,
        int thickness = 5)
    {
        if (image.IsEmpty)
            throw new ArgumentException("התמונה ריקה", nameof(image));

        var result = image.Clone();
        var drawColor = color ?? new MCvScalar(255, 0, 0); // ירוק ברירת מחדל

        for (int i = 0; i < detections.Count; i++)
        {
            var detection = detections[i];

            // המרת PointF ל-Point לצורך ציור
            var points = detection.Corners
                .Select(p => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y)))
                .ToArray();

            // ציור הפוליגון של המדבקה
            using var pointsVec = new VectorOfPoint(points);
            CvInvoke.Polylines(result, pointsVec, true, drawColor, thickness);

            // ציור נקודות הפינות
            foreach (var corner in detection.Corners)
            {
                CvInvoke.Circle(
                    result,
                    new Point((int)corner.X, (int)corner.Y),
                    10,
                    new MCvScalar(255, 0, 0), // כחול
                    -1);
            }

            // הוספת טקסט עם מידע
            var center = detection.Center;
            string infoText = $"#{i + 1} {detection.Confidence:P0}";
            string matchesText = $"Matches: {detection.Inliers}/{detection.TotalMatches}";

            int textY = (int)center.Y - 40;
            int textX = (int)center.X - 100;

            // רקע ירוק לטקסט
            CvInvoke.Rectangle(
                result,
                new Rectangle(textX, textY - 60, 200, 70),
                drawColor,
                -1);

            // טקסט שחור על רקע ירוק
            CvInvoke.PutText(
                result,
                infoText,
                new Point(textX + 10, textY - 30),
                FontFace.HersheySimplex,
                1.0,
                new MCvScalar(0, 0, 0),
                2);

            CvInvoke.PutText(
                result,
                matchesText,
                new Point(textX + 10, textY),
                FontFace.HersheySimplex,
                0.7,
                new MCvScalar(0, 0, 0),
                2);
            
        }

        return result;
    }

    /// <summary>
    /// שחרור משאבים
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _template?.Dispose();
        _templateGray?.Dispose();
        _sift?.Dispose();
        _templateKeypoints?.Dispose();
        _templateDescriptors?.Dispose();
        _matcher?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~AdvancedStickerDetector()
    {
        Dispose();
    }
}