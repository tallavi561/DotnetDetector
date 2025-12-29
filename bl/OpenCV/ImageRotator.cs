using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace StickersDetector.bl.OpenCV
{
      /// <summary>
      /// Utility class for rotating images to align objects
      /// parallel to the screen axes.
      /// </summary>
      public static class ImageRotator
      {
            /// <summary>
            /// Rotates the image by the given angle (in degrees) around its center.
            /// Positive angle = clockwise rotation.
            /// </summary>
            /// <param name="image">Input image</param>
            /// <param name="rotationDeg">
            /// Rotation angle in degrees.
            /// To align an object, pass -objectRotationDeg.
            /// </param>
            /// <returns>New rotated image</returns>
            public static Mat Rotate(Mat image, double rotationDeg)
            {
                  if (image == null || image.IsEmpty)
                        throw new ArgumentException("Image is null or empty", nameof(image));

                  // Image center
                  var center = new PointF(
                      image.Width / 2f,
                      image.Height / 2f);

                  // // Rotation matrix (negative angle aligns object)
                  // using var rotationMatrix =
                  //     CvInvoke.GetRotationMatrix2D(center, rotationDeg, 1.0);

                  // var result = new Mat();

                  // CvInvoke.WarpAffine(
                  //     image,
                  //     result,
                  //     rotationMatrix,
                  //     image.Size,
                  //     Inter.Linear,
                  //     Warp.Default,
                  //     BorderType.Constant,
                  //     new MCvScalar(0, 0, 0));

                  // return result;
                  var rotationMatrix = new Mat(); 
                  CvInvoke.GetRotationMatrix2D(center, rotationDeg, 1.0, rotationMatrix);

                  using (rotationMatrix) 
                  {
                        var result = new Mat();
                        CvInvoke.WarpAffine(
                            image,
                            result,
                            rotationMatrix,
                            image.Size,
                            Inter.Linear,
                            Warp.Default,
                            BorderType.Constant,
                            new MCvScalar(0, 0, 0));

                        return result;
                  }
            }
      }
}
