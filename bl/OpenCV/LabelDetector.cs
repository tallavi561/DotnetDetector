using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DotnetDetector.Models.Labels;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.AspNetCore.Authentication;
using StickersDetector.Models;
using StickersDetector.Models.Shapes;

namespace StickersDetector.bl.OpenCV
{
    /// <summary>
    /// Detects predefined labels in images using feature matching and homography.
    /// Loads all label templates at construction time.
    /// </summary>
    public sealed class LabelDetector : IDisposable
    {
        private readonly SIFT _sift;
        private readonly Dictionary<string, LabelModel> _labels;
        private bool _disposed;

        /// <summary>
        /// Initializes the detector with known label definitions.
        /// Heavy initialization (I/O + feature extraction) happens here.
        /// </summary>
        public LabelDetector(List<InputLabelDefinition> labelDefinitions)
        {
            if (labelDefinitions == null || labelDefinitions.Count == 0)
                throw new ArgumentException("Label definitions list is empty", nameof(labelDefinitions));

            _sift = new SIFT();
            _labels = new Dictionary<string, LabelModel>();

            foreach (var def in labelDefinitions)
            {
                if (_labels.ContainsKey(def.Name))
                    throw new InvalidOperationException($"Duplicate label name: {def.Name}");

                // Load template as grayscale
                var templateGray = CvInvoke.Imread(def.ImagePath, ImreadModes.Grayscale);
                if (templateGray.IsEmpty)
                    throw new InvalidOperationException($"Failed to load label image: {def.ImagePath}");

                // Extract features
                var keypoints = new VectorOfKeyPoint();
                var descriptors = new Mat();

                _sift.DetectAndCompute(
                    templateGray,
                    null,
                    keypoints,
                    descriptors,
                    false);

                if (keypoints.Size == 0 || descriptors.IsEmpty)
                    throw new InvalidOperationException(
                        $"No features found in label image: {def.Name}");

                var model = new LabelModel(
                    def.Name,
                    templateGray,
                    keypoints,
                    descriptors);

                _labels.Add(def.Name, model);
            }
        }

        // /// <summary>
        // /// Detects multiple instances of a specific label in the given image.
        // /// Returns an empty list if none are found.
        // /// </summary>
        // public List<LabelDetectionResult> Detect(
        //     Mat image,
        //     string labelName,
        //     int minMatches = 15,
        //     double ratioThreshold = 0.7,
        //     double ransacThreshold = 5.0,
        //     double downscale = 0.5)
        // {
        //     var results = new List<LabelDetectionResult>();

        //     if (!_labels.TryGetValue(labelName, out var label))
        //     {
        //         throw new KeyNotFoundException($"Unknown label: {labelName}");
        //     }

        //     if (image == null || image.IsEmpty)
        //     {
        //         throw new ArgumentException("Input image is empty", nameof(image));
        //     }

        //     // --- Downscale for performance ---
        //     using var workImage = new Mat();
        //     CvInvoke.Resize(image, workImage, new Size(), downscale, downscale, Inter.Area);

        //     // --- Extract features from input image (Once for all potential instances) ---
        //     using var sceneKeypoints = new VectorOfKeyPoint();
        //     using var sceneDescriptors = new Mat();

        //     _sift.DetectAndCompute(workImage, null, sceneKeypoints, sceneDescriptors, false);

        //     if (sceneDescriptors.IsEmpty || sceneKeypoints.Size < minMatches)
        //     {
        //         return results;
        //     }

        //     // --- KNN matching + Lowe ratio test ---
        //     using var matcher = new BFMatcher(DistanceType.L2, crossCheck: false);
        //     using var knnMatches = new VectorOfVectorOfDMatch();
        //     matcher.KnnMatch(label.Descriptors, sceneDescriptors, knnMatches, 2, null);

        //     var remainingMatches = new List<MDMatch>();
        //     for (int i = 0; i < knnMatches.Size; i++)
        //     {
        //         using var v = knnMatches[i];
        //         if (v.Size < 2) continue;
        //         if (v[0].Distance < ratioThreshold * v[1].Distance)
        //             remainingMatches.Add(v[0]);
        //     }

        //     // --- הוספת לולאה למציאת מופעים מרובים ---
        //     while (remainingMatches.Count >= minMatches)
        //     {
        //         var srcPoints = new PointF[remainingMatches.Count];
        //         var dstPoints = new PointF[remainingMatches.Count];

        //         for (int i = 0; i < remainingMatches.Count; i++)
        //         {
        //             srcPoints[i] = label.Keypoints[remainingMatches[i].QueryIdx].Point;
        //             var p = sceneKeypoints[remainingMatches[i].TrainIdx].Point;
        //             dstPoints[i] = new PointF((float)(p.X / downscale), (float)(p.Y / downscale));
        //         }

        //         using var srcVec = new VectorOfPointF(srcPoints);
        //         using var dstVec = new VectorOfPointF(dstPoints);
        //         using var inlierMask = new Mat();

        //         using Mat homography = CvInvoke.FindHomography(
        //             srcVec,
        //             dstVec,
        //             RobustEstimationAlgorithm.Ransac,
        //             ransacThreshold,
        //             inlierMask);

        //         // אם לא נמצאה הומוגרפיה, או שאין מספיק Inliers, עוצרים
        //         if (homography == null || homography.IsEmpty) break;

        //         // שליפת ה-Mask כדי לדעת אילו נקודות נוצלו
        //         var maskBytes = new byte[inlierMask.Rows];
        //         inlierMask.CopyTo(maskBytes);
        //         int inliersCount = maskBytes.Count(b => b != 0);

        //         if (inliersCount < minMatches) break;

        //         // --- חישוב פינות המופע הנוכחי ---
        //         var tplCorners = new[] {
        //     new PointF(0, 0),
        //     new PointF(label.TemplateGray.Width, 0),
        //     new PointF(label.TemplateGray.Width, label.TemplateGray.Height),
        //     new PointF(0, label.TemplateGray.Height)
        // };

        //         using var tplVec = new VectorOfPointF(tplCorners);
        //         using var imgVec = new VectorOfPointF();
        //         CvInvoke.PerspectiveTransform(tplVec, imgVec, homography);
        //         var corners = imgVec.ToArray();

        //         // בדיקה שהזיהוי הגיוני (בתוך גבולות התמונה)
        //         bool isValid = corners.All(p => p.X >= -10 && p.X < image.Width + 10 && p.Y >= -10 && p.Y < image.Height + 10);

        //         if (isValid)
        //         {
        //             double confidence = (double)inliersCount / remainingMatches.Count;
        //             var resultCorners = corners
        //                 .Select(p => new Point2D((int)Math.Round(p.X), (int)Math.Round(p.Y)))
        //                 .ToList().AsReadOnly();

        //             results.Add(new LabelDetectionResult(
        //                 labelName: label.Name,
        //                 corners: resultCorners,
        //                 confidence: confidence,
        //                 inliers: inliersCount,
        //                 matches: remainingMatches.Count
        //             ));
        //         }

        //         // --- הצעד הקריטי: הסרת ה-Inliers מרשימת הנקודות שנותרו ---
        //         var nextIterationMatches = new List<MDMatch>();
        //         for (int i = 0; i < remainingMatches.Count; i++)
        //         {
        //             if (maskBytes[i] == 0) // אם הנקודה היא Outlier למופע הנוכחי, נשמור אותה לסיבוב הבא
        //             {
        //                 nextIterationMatches.Add(remainingMatches[i]);
        //             }
        //         }
        //         remainingMatches = nextIterationMatches;

        //         // הגנה מפני לולאה אינסופית אם ה-Mask לא מסנן כלום
        //         if (inliersCount == 0) break;
        //     }

        //     return results;
        // }
        public List<LabelDetectionResult> Detect(
    Mat image,
    string labelName,
    int minMatches = 15,
    double ratioThreshold = 0.7,
    double ransacThreshold = 5.0,
    double downscale = 0.5)
        {
            var results = new List<LabelDetectionResult>();

            if (!_labels.TryGetValue(labelName, out var label))
                throw new KeyNotFoundException($"Unknown label: {labelName}");

            if (image == null || image.IsEmpty)
                throw new ArgumentException("Input image is empty", nameof(image));

            // --- 1. הכנה ושינוי גודל ---
            using var workImage = new Mat();
            CvInvoke.Resize(image, workImage, new Size(), downscale, downscale, Inter.Area);

            // --- 2. חילוץ מאפיינים מהתמונה המלאה (פעם אחת) ---
            using var sceneKeypoints = new VectorOfKeyPoint();
            using var sceneDescriptors = new Mat();
            _sift.DetectAndCompute(workImage, null, sceneKeypoints, sceneDescriptors, false);

            if (sceneDescriptors.IsEmpty || sceneKeypoints.Size < minMatches)
                return results;

            // --- 3. התאמת נקודות (KNN) וסינון ראשוני (Lowe's Ratio) ---
            using var matcher = new BFMatcher(DistanceType.L2);
            using var knnMatches = new VectorOfVectorOfDMatch();
            matcher.KnnMatch(label.Descriptors, sceneDescriptors, knnMatches, 2);

            var remainingMatches = new List<MDMatch>();
            for (int i = 0; i < knnMatches.Size; i++)
            {
                if (knnMatches[i].Size < 2) continue;
                if (knnMatches[i][0].Distance < ratioThreshold * knnMatches[i][1].Distance)
                    remainingMatches.Add(knnMatches[i][0]);
            }

            // --- 4. לולאת איתור מופעים מרובים ---
            while (remainingMatches.Count >= minMatches)
            {
                var srcPoints = new PointF[remainingMatches.Count];
                var dstPoints = new PointF[remainingMatches.Count];

                for (int i = 0; i < remainingMatches.Count; i++)
                {
                    srcPoints[i] = label.Keypoints[remainingMatches[i].QueryIdx].Point;
                    var p = sceneKeypoints[remainingMatches[i].TrainIdx].Point;
                    // המרה חזרה לקואורדינטות של התמונה המקורית (לפני Downscale)
                    dstPoints[i] = new PointF((float)(p.X / downscale), (float)(p.Y / downscale));
                }

                using var srcVec = new VectorOfPointF(srcPoints);
                using var dstVec = new VectorOfPointF(dstPoints);
                using var inlierMask = new Mat();
                using var homography = CvInvoke.FindHomography(srcVec, dstVec, RobustEstimationAlgorithm.Ransac, ransacThreshold, inlierMask);

                if (homography == null || homography.IsEmpty) break;

                // ספירת Inliers של RANSAC
                var maskBytes = new byte[inlierMask.Rows];
                inlierMask.CopyTo(maskBytes);
                int inliersCount = maskBytes.Count(b => b != 0);

                if (inliersCount < minMatches) break;

                // חישוב פינות המדבקה בתמונה
                var tplCorners = new[] {
            new PointF(0, 0),
            new PointF(label.TemplateGray.Width, 0),
            new PointF(label.TemplateGray.Width, label.TemplateGray.Height),
            new PointF(0, label.TemplateGray.Height)
        };

                using var tplVec = new VectorOfPointF(tplCorners);
                using var imgVec = new VectorOfPointF();
                CvInvoke.PerspectiveTransform(tplVec, imgVec, homography);
                var cornersArray = imgVec.ToArray();

                // --- בדיקות איכות גיאומטריות ---
                double area = CvInvoke.ContourArea(imgVec);
                bool isConvex = CvInvoke.IsContourConvex(imgVec);
                bool isInBounds = cornersArray.All(p => p.X >= -50 && p.X < image.Width + 50 && p.Y >= -50 && p.Y < image.Height + 50);

                // שטח מינימלי (למשל 100 פיקסלים) כדי למנוע רעש
                if (isConvex && area > 100 && isInBounds)
                {
                    results.Add(new LabelDetectionResult(
                        labelName: label.Name,
                        corners: cornersArray.Select(p => new Point2D((int)Math.Round(p.X), (int)Math.Round(p.Y))).ToList().AsReadOnly(),
                        confidence: (double)inliersCount / remainingMatches.Count,
                        inliers: inliersCount,
                        matches: remainingMatches.Count
                    ));
                }

                // --- הצעד הקריטי: הסרה גיאומטרית של נקודות ---
                // אנחנו מסירים כל נקודה שנמצאת בתוך הפוליגון שמצאנו, גם אם היא לא הייתה Inlier רשמי
                var nextIterationMatches = new List<MDMatch>();
                for (int i = 0; i < remainingMatches.Count; i++)
                {
                    var p = sceneKeypoints[remainingMatches[i].TrainIdx].Point;
                    var scenePtOriginalScale = new PointF((float)(p.X / downscale), (float)(p.Y / downscale));

                    // בדיקה האם הנקודה בתוך הפוליגון (ערך >= 0 אומר בפנים או על הקו)
                    if (CvInvoke.PointPolygonTest(imgVec, scenePtOriginalScale, false) < 0)
                    {
                        nextIterationMatches.Add(remainingMatches[i]);
                    }
                }

                // הגנה מפני לולאה אינסופית: אם לא הצלחנו לצמצם את הרשימה, נפסיק
                if (nextIterationMatches.Count == remainingMatches.Count) break;

                remainingMatches = nextIterationMatches;
            }

            return results;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var label in _labels.Values)
                label.Dispose();

            _sift.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
