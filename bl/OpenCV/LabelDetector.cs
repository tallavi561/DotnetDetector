using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using DotnetDetector.Models.Labels;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using StickersDetector.Models;
using StickersDetector.Models.Shapes;

namespace StickersDetector.bl.OpenCV
{
    public sealed class LabelDetector : IDisposable
    {
        private readonly SIFT _sift;
        private readonly Dictionary<string, LabelModel> _labels;
        private bool _disposed;

        public LabelDetector(List<InputLabelDefinition> labelDefinitions)
        {
            if (labelDefinitions == null || labelDefinitions.Count == 0)
                throw new ArgumentException("Label definitions list is empty", nameof(labelDefinitions));

            // Constructor timing is not a priority. 
            // Lowering nFeatures to 800 significantly speeds up the 'Detect' phase.
            _sift = new SIFT(nFeatures: 1400, nOctaveLayers: 3, contrastThreshold: 0.04, edgeThreshold: 10, sigma: 1.6);
            _labels = new Dictionary<string, LabelModel>();

            foreach (var def in labelDefinitions)
            {
                using var templateGray = CvInvoke.Imread(def.ImagePath, ImreadModes.Grayscale);
                if (templateGray.IsEmpty) continue;

                var keypoints = new VectorOfKeyPoint();
                var descriptors = new Mat();
                _sift.DetectAndCompute(templateGray, null, keypoints, descriptors, false);

                _labels.Add(def.Name, new LabelModel(def.Name, templateGray.Clone(), keypoints, descriptors));
            }
        }

        public List<LabelDetectionResult> Detect(
          Mat image,
          string labelName,
          int minMatches = 15,
          double ratioThreshold = 0.7,
          double ransacThreshold = 5.0,
          int maxProcessingDimension = 1000)
        {
            var results = new List<LabelDetectionResult>();
            if (image == null || image.IsEmpty) return results;
            if (!_labels.TryGetValue(labelName, out var label)) return results;

            // --- 1. Dynamic Resize (Performance Gain) ---
            double scale = CalculateScale(image.Size, maxProcessingDimension);
            using var workImage = new Mat();
            CvInvoke.Resize(image, workImage, new Size(), scale, scale, Inter.Area);

            // --- 2. SIFT Extraction (The Main Bottleneck) ---
            using var sceneKeypoints = new VectorOfKeyPoint();
            using var sceneDescriptors = new Mat();
            _sift.DetectAndCompute(workImage, null, sceneKeypoints, sceneDescriptors, false);

            if (sceneDescriptors.IsEmpty || sceneKeypoints.Size < minMatches)
                return results;

            // --- 3 & 4. Matching & Ratio Test ---
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

            // --- 5. Multi-instance Loop ---
            while (remainingMatches.Count >= minMatches)
            {
                var srcPoints = new PointF[remainingMatches.Count];
                var dstPoints = new PointF[remainingMatches.Count];

                for (int i = 0; i < remainingMatches.Count; i++)
                {
                    srcPoints[i] = label.Keypoints[remainingMatches[i].QueryIdx].Point;
                    var p = sceneKeypoints[remainingMatches[i].TrainIdx].Point;
                    dstPoints[i] = new PointF((float)(p.X / scale), (float)(p.Y / scale));
                }

                using var srcVec = new VectorOfPointF(srcPoints);
                using var dstVec = new VectorOfPointF(dstPoints);
                using var inlierMask = new Mat();
                using var homography = CvInvoke.FindHomography(srcVec, dstVec, RobustEstimationAlgorithm.Ransac, ransacThreshold, inlierMask);

                if (homography == null || homography.IsEmpty) break;

                int inliersCount = CountInliers(inlierMask);
                if (inliersCount < minMatches) break;

                var tplCorners = new[] {
            new PointF(0, 0),
            new PointF(label.TemplateGray.Width, 0),
            new PointF(label.TemplateGray.Width, label.TemplateGray.Height),
            new PointF(0, label.TemplateGray.Height)
        };

                using var tplVec = new VectorOfPointF(tplCorners);
                using var imgVec = new VectorOfPointF();
                CvInvoke.PerspectiveTransform(tplVec, imgVec, homography);

                if (CvInvoke.IsContourConvex(imgVec) && CvInvoke.ContourArea(imgVec) > 100)
                {
                    var cornersArray = imgVec.ToArray();
                    results.Add(new LabelDetectionResult(
                        labelName: label.Name,
                        corners: cornersArray.Select(p => new Point2D((int)Math.Round(p.X), (int)Math.Round(p.Y))).ToList().AsReadOnly(),
                        confidence: (double)inliersCount / remainingMatches.Count,
                        inliers: inliersCount,
                        matches: remainingMatches.Count
                    ));
                }
                else
                {
                    // אם הפוליגון לא תקין, צא מהלולאה
                    break;
                }

                // --- Geometric Filter: הסר רק inliers שנמצאו ---
                var nextIterationMatches = new List<MDMatch>();
                byte[] maskBytes = new byte[inlierMask.Rows];
                inlierMask.CopyTo(maskBytes);

                for (int i = 0; i < remainingMatches.Count; i++)
                {
                    // שמור רק outliers (matches שלא שימשו בהומוגרפיה הנוכחית)
                    if (maskBytes[i] == 0)
                    {
                        nextIterationMatches.Add(remainingMatches[i]);
                    }
                }

                // אם לא הצלחנו להסיר matches, צא מהלולאה
                if (nextIterationMatches.Count == remainingMatches.Count) break;

                remainingMatches = nextIterationMatches;
            }

            return results;
        }

        // --- PRIVATE HELPERS (Fixed CS0103) ---

        private double CalculateScale(Size size, int maxDim)
        {
            if (size.Width <= maxDim && size.Height <= maxDim) return 1.0;
            return (double)maxDim / Math.Max(size.Width, size.Height);
        }

        private int CountInliers(Mat mask)
        {
            if (mask == null || mask.IsEmpty) return 0;
            var maskBytes = new byte[mask.Rows];
            mask.CopyTo(maskBytes);
            return maskBytes.Count(b => b != 0);
        }

        public void Dispose()
        {
            if (_disposed) return;
            foreach (var label in _labels.Values) label.Dispose();
            _sift.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}