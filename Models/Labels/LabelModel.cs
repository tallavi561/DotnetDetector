using System;
using Emgu.CV;
using Emgu.CV.Util;

namespace DotnetDetector.Models.Labels
{
    /// <summary>
    /// Represents a loaded label template with precomputed visual features.
    /// This object is created once at startup and reused during detection.
    /// </summary>
    public sealed class LabelModel : IDisposable
    {
        /// <summary>
        /// Logical name of the label (e.g. "A", "B", "FRAGILE").
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Grayscale template image of the label.
        /// </summary>
        public Mat TemplateGray { get; }

        /// <summary>
        /// Keypoints extracted from the template image.
        /// </summary>
        public VectorOfKeyPoint Keypoints { get; }

        /// <summary>
        /// Feature descriptors extracted from the template image.
        /// </summary>
        public Mat Descriptors { get; }

        public LabelModel(
            string name,
            Mat templateGray,
            VectorOfKeyPoint keypoints,
            Mat descriptors)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TemplateGray = templateGray ?? throw new ArgumentNullException(nameof(templateGray));
            Keypoints = keypoints ?? throw new ArgumentNullException(nameof(keypoints));
            Descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        }

        public void Dispose()
        {
            TemplateGray.Dispose();
            Keypoints.Dispose();
            Descriptors.Dispose();
        }
    }
}
