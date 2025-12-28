


using Emgu.CV.CvEnum;

using Emgu.CV.Structure;
using Emgu.CV;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO.Packaging;
using Emgu.CV.Util;
using Emgu.CV.Cuda;
using System.Windows.Forms;
///using OpenCvSharp;
//using static OpenCvSharp.FileStorage;
//using Point = OpenCvSharp.Point;
//using Size = OpenCvSharp.Size;
//using Mat = Emgu.CV.Mat;
//using ImreadModes = Emgu.CV.CvEnum.ImreadModes;
namespace FindLabel
{
    public partial class Form1 : Form
    {
        int filePointer = 0;
        string folderPath = "C:\\Users\\avshalom.lavi.okabi\\Downloads\\20251211_Bitunia_1\\";

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // string inputImage = "D:\\WaterTemp\\sample_1.jpg";

            string inputImage = "D:\\WaterTemp\\Packages\\s_2.jpg";
            string outputImage = "D:\\WaterTemp\\Labels\\s_2_detected.jpg";

            RunChatGptSample1();

            //MainProgramDetectWhiteStickers(inputImage, outputImage);
            // MainProgramFindLabels("D:\\WaterTemp\\Packages\\", "D:\\WaterTemp\\Labels\\");

        }
        static void RunChatGptSample1()
        {
            string[] files =
           {
                "20251211_Image102925_Bitunia_1_0000000126_0126.jpg",
                "20251211_Image103915_Bitunia_1_0000000127_0127.jpg",
                "20251211_Image103918_Bitunia_1_0000000128_0128.jpg",
                "20251211_Image103921_Bitunia_1_0000000129_0129.jpg",
                "20251211_Image102819_Bitunia_1_0000000118_0118.jpg",
                "20251211_Image102821_Bitunia_1_0000000119_0119.jpg",
                "20251211_Image102828_Bitunia_1_0000000121_0121.jpg",
                "20251211_Image102832_Bitunia_1_0000000122_0122.jpg",
                "20251211_Image102840_Bitunia_1_0000000123_0123.jpg",
                "20251211_Image103927_Bitunia_1_0000000131_0131.jpg"
            };
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var file in files)
            {
                string output = "C:\\Users\\avshalom.lavi.okabi\\Downloads\\20251211_Bitunia_1\\OUT_" + file;
                //  ChaGptSample3("C:\\Users\\avshalom.lavi.okabi\\Downloads\\20251211_Bitunia_1\\" + file, output);
                /*  LabelDetector.ProcessImage("C:\\Users\\avshalom.lavi.okabi\\Downloads\\20251211_Bitunia_1\\" + file,
                      output, "Test");*/

                /*    OpenCVLabelSigner2.ProcessImage("C:\\Users\\avshalom.lavi.okabi\\Downloads\\20251211_Bitunia_1\\" + file,
                        output, "Test");*/
                // GeminiSamples("C:\\Users\\avshalom.lavi.okabi\\Downloads\\20251211_Bitunia_1\\" + file, output);
            }
            sw.Stop();
        }

        /* static void GeminiSamples(string inputPath, string outputPath)
         {
             // טען את התמונה
             string imagePath = inputPath;
             Mat image = CvInvoke.Imread(imagePath, ImreadModes.Grayscale);
             // הפוך את התמונה לבינארית
             Mat binaryImage = new Mat();
             CvInvoke.Threshold(image, binaryImage, 200, 255, ThresholdType.Binary);
             // מצא את המדבקות (קונטורים)
             using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
             {
                 Mat hierarchy = new Mat();
                 CvInvoke.FindContours(binaryImage, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                 // עבור על הקונטורים ומצא את המדבקות
                 for (int i = 0; i < contours.Size; i++)
                 {
                     // חשב את שטח הקונטור
                     double area = CvInvoke.ContourArea(contours[i]);
                     // בדוק אם השטח גדול מספיק (כדי להימנע מקונטורים קטנים)
                     if (area > 100)
                     // שנה את הערך לפי הצורך
                     {
                         // צייר את הקונטור על התמונה
                         Rectangle boundingBox = CvInvoke.BoundingRectangle(contours[i]);
                         // צייר את המלבן על התמונה
                         CvInvoke.Rectangle(image, boundingBox, new MCvScalar(0, 0, 255), 2);
                         // אדום               
                     }
                 }
             }
             // הצג את התמונה עם המדבקות המסומנות
             //CvInvoke.Imshow("Detected Stickers", image);
             CvInvoke.Imwrite(outputPath, image);
             CvInvoke.WaitKey(0);
         }
         static void ChaGptSample3(string inputPath, string outputPath)
         {

             Mat img = CvInvoke.Imread(inputPath, ImreadModes.Grayscale);
             Mat color = new Mat();
             CvInvoke.CvtColor(img, color, ColorConversion.Gray2Bgr);

             // HOG descriptor
             HOGDescriptor hog = new HOGDescriptor(
                 new Size(64, 128),
                 new Size(16, 16),
                 new Size(8, 8),
                 new Size(8, 8),
                 9
             );

             // Pre-trained SVM (Emgu built-in people detector)
             // Surprisingly effective for rectangular bright labels
             hog.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());

             List<Rectangle> labels = new List<Rectangle>();

             // Slide over whole image
             for (int y = 0; y < img.Height - 128; y += 40)
             {
                 for (int x = 0; x < img.Width - 64; x += 40)
                 {
                     Rectangle win = new Rectangle(x, y, 64, 128);
                     Mat roi = new Mat(img, win);

                     float[] descriptor = hog.Compute(roi);

                     // very simple scoring based on HOG pattern density
                     double sum = 0;
                     for (int i = 0; i < descriptor.Length; i++)
                         sum += Math.Abs(descriptor[i]);

                     // label tends to have low texture density
                     if (sum < 15000) // tuned threshold
                     {
                         labels.Add(win);
                     }
                 }
             }

             // merge overlapping windows
             List<Rectangle> finalLabels = MergeWindows(labels);

             foreach (var rect in finalLabels)
             {
                 CvInvoke.Rectangle(color, rect, new MCvScalar(0, 0, 255), 5);
             }

             CvInvoke.Imwrite(outputPath, color);
             Console.WriteLine("Detected labels: " + finalLabels.Count);

         }*/

        private static List<Rectangle> MergeWindows(List<Rectangle> windows)
        {
            List<Rectangle> result = new List<Rectangle>();
            foreach (var r in windows)
            {
                bool merged = false;
                for (int i = 0; i < result.Count; i++)
                {
                    Rectangle rr = result[i];
                    if (IsOverlap(r, rr))
                    {
                        result[i] = Rectangle.Union(r, rr);
                        merged = true;
                        break;
                    }
                }
                if (!merged)
                    result.Add(r);
            }
            return result;
        }

        private static bool IsOverlap(Rectangle a, Rectangle b)
        {
            return a.IntersectsWith(b) || a.Contains(b) || b.Contains(a);
        }
        /*
        static void ChatGptSample2(string inputPath, string outputPath)
        {

            // Load grayscale
            Mat gray = CvInvoke.Imread(inputPath, ImreadModes.Grayscale);

            // Enhance contrast
            Mat clahe = new Mat();
            CvInvoke.CLAHE(gray, 3.0, new Size(8, 8), clahe);

            // Smooth noise
            Mat blur = new Mat();
            CvInvoke.GaussianBlur(clahe, blur, new Size(5, 5), 0);

            // Use Otsu threshold
            Mat bin = new Mat();
            CvInvoke.Threshold(blur, bin, 0, 255, ThresholdType.Otsu | ThresholdType.BinaryInv);

            // Close holes (connect label parts)
            Mat morph = new Mat();
            Mat kernel = CvInvoke.GetStructuringElement(MorphShapes.Rectangle, new Size(25, 25), new Point(-1, -1));
            CvInvoke.MorphologyEx(bin, morph, MorphOp.Close, kernel, new Point(-1, -1), 2, BorderType.Reflect, new MCvScalar());

            // Find contours
            var contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
            CvInvoke.FindContours(morph, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            // Prepare output image
            Mat output = new Mat();
            CvInvoke.CvtColor(gray, output, ColorConversion.Gray2Bgr);

            int detected = 0;

            for (int i = 0; i < contours.Size; i++)
            {
                var contour = contours[i];

                Rectangle rect = CvInvoke.BoundingRectangle(contour);
                double area = rect.Width * rect.Height;

                // Remove tiny blobs
                if (area < 20000)
                    continue;

                // Remove huge false areas
                if (area > gray.Width * gray.Height * 0.9)
                    continue;

                // Aspect ratio filter
                double aspectRatio = rect.Width / (double)rect.Height;
                if (aspectRatio < 0.3 || aspectRatio > 6.0)
                    continue;

                // Solidity filter (labels are "solid")
                double contourArea = CvInvoke.ContourArea(contour);
                var hull = new Emgu.CV.Util.VectorOfPoint();
                CvInvoke.ConvexHull(contour, hull);
                double hullArea = CvInvoke.ContourArea(hull);

                if (hullArea > 0)
                {
                    double solidity = contourArea / hullArea;
                    if (solidity < 0.45)
                        continue;
                }

                // Draw rectangle
                CvInvoke.Rectangle(output, rect, new MCvScalar(0, 0, 255), 5);

                detected++;
            }

            CvInvoke.Imwrite(outputPath, output);
            Console.WriteLine($"Detected {detected} labels -> {outputPath}");

        }
        */
        static void ChatGptSample1(string inputPath, string outputPath)
        {
            /*
                 if (!File.Exists(inputPath))
                 {
                     Console.WriteLine("Image not found: " + inputPath);
                     return;
                 }

                 // Load grayscale
                 Mat img = Cv2.ImRead(inputPath, ImreadModes.Grayscale);

                 // Improve contrast (VERY important for your monochrome images)
                 Mat claheImg = new Mat();
                 var clahe = Cv2.CreateCLAHE(clipLimit: 3.0, tileGridSize: new Size(8, 8));
                 clahe.Apply(img, claheImg);

                 // Adaptive threshold for uneven lighting
                 Mat thresh = new Mat();
                 Cv2.AdaptiveThreshold(
                     claheImg, thresh, 255,
                     AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv,
                     41, 5
                 );

                 // Close small gaps
                 Mat morph = new Mat();
                 Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(25, 25));
                 Cv2.MorphologyEx(thresh, morph, MorphTypes.Close, kernel);

                 // Find contours
                 Cv2.FindContours(
                     morph,
                     out Point[][] contours,
                     out HierarchyIndex[] hierarchy,
                     RetrievalModes.External,
                     ContourApproximationModes.ApproxSimple
                 );

                 // Convert original to color for drawing boxes
                 Mat output = new Mat();
                 Cv2.CvtColor(img, output, ColorConversionCodes.GRAY2BGR);

                 int count = 0;

                 foreach (var cnt in contours)
                 {
                     double area = Cv2.ContourArea(cnt);
                     if (area < 20000 || area > img.Width * img.Height * 0.9)
                         continue;

                     Rect rect = Cv2.BoundingRect(cnt);

                     // Reject extremely thin/noisy shapes
                     double aspect = rect.Width / (double)rect.Height;
                     if (aspect < 0.3 || aspect > 5.5)
                         continue;

                     // Check solidity (label is usually solid rectangular blob)
                     double hullArea = Cv2.ContourArea(Cv2.ConvexHull(cnt));
                     double solidity = area / hullArea;
                     if (solidity < 0.45)
                         continue;

                     // Draw rectangle
                     Cv2.Rectangle(output, rect, new Scalar(0, 0, 255), 4);
                     count++;
                 }

                 Cv2.ImWrite(outputPath, output);
                 Console.WriteLine($"Detected {count} labels → saved to {outputPath}");*/

        }
        static void MainProgramFindLabels(string inputFolder, string outputFolder)
        {
            /* // שימוש:
             // LabelDetector.exe <inputFolder> <outputFolder>


            // string inputFolder = args[0];
            // string outputFolder = args[1];

             if (!Directory.Exists(inputFolder))
             {
                 Console.WriteLine("Input folder not found");
                 return;
             }


             if (!Directory.Exists(outputFolder))
             {
                 Directory.CreateDirectory(outputFolder);
             }

             string[] imageFiles = Directory.GetFiles(inputFolder, "*.jpg")
                                            ?? Array.Empty<string>();

             foreach (string file in imageFiles)
             {
                 Mat src = Cv2.ImRead(file);
                 if (src.Empty())
                 {
                     Console.WriteLine($"Cannot read image {file}");
                     continue;
                 }

                 // List<Rect> labels = FindLabels(src);
                // List<Rect> labels = DetectLabels(src);
                DetectLabels(file, Path.Combine(outputFolder, "debug_" + Path.GetFileName(file)));
               //  var dbg = DrawDebug(src, labels);
              //   Cv2.ImWrite(Path.Combine(outputFolder, "debug_" + Path.GetFileName(file)), dbg);

                  Console.WriteLine($"{Path.GetFileName(file)} : found {labels.Count} labels");

                for (int i = 0; i < labels.Count; i++)
                 {
                     Rect r = ExpandRect2(labels[i], src.Width, src.Height, 0.05); // קצת מרווח מסביב

                     using Mat crop = new Mat(src, r);
                     string outPath = Path.Combine(
                         outputFolder,
                         $"{Path.GetFileNameWithoutExtension(file)}_label_{i + 1}.jpg");

                     Cv2.ImWrite(outPath, crop);
                 }

                 src.Dispose();
             }

             Console.WriteLine("Done.");*/
        }
        static void MainProgramDetectWhiteStickers(string inputImage, string outputImage)
        {
            /*  Mat img = Cv2.ImRead(inputImage);
              if (img.Empty())
              {
                  Console.WriteLine("Cannot open image: " + inputImage);
                  return;
              }

              var results = DetectWhiteStickers(img);
              Console.WriteLine($"Found {results.Count} candidate(s).");
              foreach (var r in results)
              {
                  Cv2.Rectangle(img, r, Scalar.Red, 2);
              }
              string outName = outputImage;
              Cv2.ImWrite(outName, img);*/

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string[] files =
            {
                "0000000035_0035_TO_20251225_124320.jpg",
                "0000000034_0034_TO_20251225_124317.jpg",
                "0000000033_0033_TO_20251225_124313.jpg",
                "0000000032_0032_TO_20251225_124310.jpg",
                "0000000031_0031_TO_20251225_124304.jpg",
                "0000000030_0030_TO_20251225_124259.jpg",
                "20251211_Image102925_Bitunia_1_0000000126_0126.jpg",
                "20251211_Image103915_Bitunia_1_0000000127_0127.jpg",
                "20251211_Image103918_Bitunia_1_0000000128_0128.jpg",
                "20251211_Image103921_Bitunia_1_0000000129_0129.jpg",
                "20251211_Image102819_Bitunia_1_0000000118_0118.jpg",
                "20251211_Image102821_Bitunia_1_0000000119_0119.jpg",
                "20251211_Image102828_Bitunia_1_0000000121_0121.jpg",
                "20251211_Image102832_Bitunia_1_0000000122_0122.jpg",
                "20251211_Image102840_Bitunia_1_0000000123_0123.jpg",
                "20251211_Image103927_Bitunia_1_0000000131_0131.jpg"
            };
            Emgu.CV.Mat img = CvInvoke.Imread(folderPath + files[filePointer], Emgu.CV.CvEnum.ImreadModes.Grayscale);

            //OpenCVLabelSigner2.ProcessImage(folderPath + files[filePointer], folderPath, "");
            //EmguCVSample.Main(folderPath + files[filePointer]);
            originalPicture.Image = img.ToBitmap();
            originalPicture.SizeMode = PictureBoxSizeMode.StretchImage;
            string res = StickerDetectionApp.Main(Path.Combine(txtPackageLocation.Text, files[filePointer]),
                txtOutputLocation.Text, cmbPattern.SelectedItem.ToString(),this);
            if (res != "")
            {
                img = CvInvoke.Imread(res, Emgu.CV.CvEnum.ImreadModes.AnyColor);
                processedPicture.Image = img.ToBitmap();
                processedPicture.SizeMode = PictureBoxSizeMode.StretchImage;
            }
            filePointer++;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Detect white sticker candidates in the image and return bounding rectangles.
        /// </summary>
        /* static List<Rect> DetectWhiteStickers(Mat src)
         {   List<Rect> candidates = new List<Rect>();
             // פרמטרים שאפשר לכוונן לפי תאורה וסוג המדבקות:
            int cannyThreshold1 = 50;
             int cannyThreshold2 = 150;
             int minArea = 20000;      // מינימום שטח של מועמד (פיקסלים)
             int maxArea = 20000000;   // מקסימום שטח (להוציא שוליים גדולים)
             double minAspect = 0.3; // יחס צורה מינימלי (width/height)
             double maxAspect = 4.0; // יחס צורה מקסימלי

             Mat img = new Mat();
             src.CopyTo(img);

             // אופציה: חשב תאורת אחידות (שיפור קונטרסט) - CLAHE על הערוץ הבהיר ב-Lab
             Mat lab = new Mat();
             Cv2.CvtColor(img, lab, ColorConversionCodes.BGR2Lab);
             var labChannels = Cv2.Split(lab);
             var clahe = Cv2.CreateCLAHE(clipLimit: 3.0, tileGridSize: new Size(8, 8));
             Mat lClahe = new Mat();
             clahe.Apply(labChannels[0], lClahe);
             labChannels[0] = lClahe;
             Cv2.Merge(labChannels, lab);
             Mat enhanced = new Mat();
             Cv2.CvtColor(lab, enhanced, ColorConversionCodes.Lab2BGR);

             // המרה ל-HSV
             Mat hsv = new Mat();
             Cv2.CvtColor(enhanced, hsv, ColorConversionCodes.BGR2HSV);

             // נבחר סף עבור "לבן": S קטן (מעט צבע), V גבוה (בהיר)
             // ניתן לכוון את הספים בהתאם לתאורה והגוונים של הרקע
             Scalar lowerWhite = new Scalar(0, 0, 180);   // H don't care, S 0..?, V >= 180
             Scalar upperWhite = new Scalar(180, 60, 255);
            // Scalar upperWhite = new Scalar(128, 9, 179);

             Mat mask = new Mat();
             Cv2.InRange(hsv, lowerWhite, upperWhite, mask);

             // נקו רעשים
             Mat kern = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
             Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kern, iterations: 2);
             Cv2.MorphologyEx(mask, mask, MorphTypes.Open, kern, iterations: 1);

             // אופציונלי: חישוב קונטורים ישירות על ה-mask
             OpenCvSharp.Point[][] contours;
             HierarchyIndex[] hierarchy;
             Cv2.FindContours(mask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

             

             for (int i = 0; i < contours.Length; i++)
             {
                 double area = Cv2.ContourArea(contours[i]);
                 if (area < minArea || area > maxArea) continue;

                 Rect rect = Cv2.BoundingRect(contours[i]);

                 double aspect = (double)rect.Width / rect.Height;
                 if (aspect < minAspect || aspect > maxAspect) continue;

                 // אפשר לבדוק מידה של מלבניות: יחס שטח קונטור/שטח תיבה
                 double rectArea = rect.Width * rect.Height;
                 double fillRatio = area / rectArea;
                 if (fillRatio < 0.4) continue; // אם הקונטור מפוזר מאוד, לדחות

                 // אופציונלי: בדיקת שכבת צבע בפנים כדי לוודא שהאזור באמת 'לבן'
                 Mat roi = new Mat(src, rect);
                 Mat roiHsv = new Mat();
                 Cv2.CvtColor(roi, roiHsv, ColorConversionCodes.BGR2HSV);
                 Mat roiMask = new Mat();
                 Cv2.InRange(roiHsv, lowerWhite, upperWhite, roiMask);
                 double whitePixels = Cv2.CountNonZero(roiMask);
                 double roiTotal = roi.Rows * roi.Cols;
                 if (roiTotal > 0 && (whitePixels / roiTotal) < 0.35) continue; // צריך להיות אחוז לא רע של לבן

                 candidates.Add(rect);
             }

             return candidates;
         }

         /// <summary>
         /// פונקציה שמוצאת מלבנים שנראים כמו מדבקות לבנות על החבילה.
         /// </summary>
         static List<Rect> FindLabels(Mat src)
         {
             var result = new List<Rect>();

             using var gray = new Mat();
             using var blurred = new Mat();
             using var binary = new Mat();

             // הפיכה לאפור
             Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

             // טשטוש לריכוך רעש
             Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);

             // התאמת סף אדפטיבי – הופך את המדבקות ללבן והרקע לשחור
             Cv2.AdaptiveThreshold(
                 blurred,
                 binary,
                 255,
                 AdaptiveThresholdTypes.MeanC,
                 ThresholdTypes.BinaryInv,
                 31,
                 10);

             // סגירה מורפולוגית לסגירת חורים בטקסט
             using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(7, 7));
             Cv2.MorphologyEx(binary, binary, MorphTypes.Close, kernel, iterations: 2);

             // חיפוש קונטורים
             Cv2.FindContours(
                 binary,
                 out Point[][] contours,
                 out _,
                 RetrievalModes.External,
                 ContourApproximationModes.ApproxSimple);

             double imgArea = src.Rows * src.Cols;

             foreach (var contour in contours)
             {
                 double area = Cv2.ContourArea(contour);
                 if (area < imgArea * 0.005 || area > imgArea * 0.6)
                     continue; // קטן מדי או גדול מדי

                 // קירוב לצורה פוליגונלית (מטרה: מלבן)
                 Point[] approx = Cv2.ApproxPolyDP(
                     contour,
                     0.03 * Cv2.ArcLength(contour, true),
                     true);

                 if (approx.Length < 4 || approx.Length > 10)
                     continue; // לא מלבני מספיק

                 Rect rect = Cv2.BoundingRect(approx);

                 double aspect = rect.Width / (double)rect.Height;
                 if (aspect < 0.5 || aspect > 4.0)
                     continue; // מדבקות בדרך כלל לא צרות/ארוכות מדי

                 // בדיקה שהאזור בהיר יחסית (מדבקה לבנה על רקע חום/אפור)
                 using var roi = new Mat(gray, rect);
                 Scalar mean = Cv2.Mean(roi);
                 if (mean.Val0 < 140)   // אם כהה מדי – כנראה לא מדבקה לבנה
                     continue;

                 result.Add(rect);
             }

             return result;
         }
         static List<Rect> DetectLabels(Mat src)
         {
             var result = new List<Rect>();

             using var gray = new Mat();
             using var blur = new Mat();
             using var edges = new Mat();
             using var dilated = new Mat();
             using var morph = new Mat();

             // המרה לאפור
             Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

             // טשטוש
             Cv2.GaussianBlur(gray, blur, new Size(5, 5), 1);

             // קצוות (הכי טוב במדבקות)
             //Cv2.Canny(blur, edges, 25, 110);
             Cv2.Canny(blur, edges, 15, 100);

             // הרחבה
             Mat k1 = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
             Cv2.Dilate(edges, dilated, k1, iterations: 3);

             // סגירת חורים — המדבקה הופכת גוש אחד
             Cv2.MorphologyEx(dilated, morph, MorphTypes.Close, k1, iterations: 2);

             // קונטורים
             Cv2.FindContours(morph, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

             double imgArea = src.Width * src.Height;

             foreach (var contour in contours)
             {
                 double area = Cv2.ContourArea(contour);

                 // גודל רגיל של מדבקות משלוח
                 if (area < imgArea * 0.002 || area > imgArea * 0.45)
                     continue;

                 var approx = Cv2.ApproxPolyDP(contour, 0.02 * Cv2.ArcLength(contour, true), true);

                 if (approx.Length < 4 || approx.Length > 12)
                     continue;

                 Rect r = Cv2.BoundingRect(approx);

                 // יחס מדבקה
                 double asp = (double)r.Width / r.Height;
                 if (asp < 0.3 || asp > 5.5)
                     continue;

                 // בדיקה בסיסית של בהירות
                 using var roi = new Mat(gray, r);
                 double m = Cv2.Mean(roi).Val0;
                 if (m < 100)
                     continue;

                 result.Add(r);
             }

             return result;
         }

         static List<Rect> DetectLabels2(Mat src)
         {
             var result = new List<Rect>();

             using var gray = new Mat();
             using var blur = new Mat();
             using var edges = new Mat();
             using var dilated = new Mat();
             using var closed = new Mat();

             // 1. המרת צבע -> אפור
             Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

             // 2. טשטוש חזק נגד רעש
             Cv2.GaussianBlur(gray, blur, new Size(7, 7), 1);

             // 3. Canny חזק לזיהוי גבולות של מדבקות
             Cv2.Canny(blur, edges, 40, 150);

             // 4. הרחבה — כדי לאחד את גבולות המדבקה
             Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
             Cv2.Dilate(edges, dilated, kernel, iterations: 3);

             // 5. סגירת חורים — המדבקה תהיה מלבן מלא
             Cv2.MorphologyEx(dilated, closed, MorphTypes.Close, kernel, iterations: 3);

             // 6. מציאת קונטורים
             Cv2.FindContours(closed, out Point[][] contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

             double imgArea = src.Width * src.Height;

             foreach (var contour in contours)
             {
                 // 7. סינון לפי גודל
                 double area = Cv2.ContourArea(contour);
                 if (area < imgArea * 0.004 || area > imgArea * 0.5)
                     continue;

                 // 8. קירוב לפוליגון
                 var approx = Cv2.ApproxPolyDP(contour, 0.03 * Cv2.ArcLength(contour, true), true);

                 // חייב להיות מלבני
                 if (approx.Length < 4 || approx.Length > 8)
                     continue;

                 Rect box = Cv2.BoundingRect(approx);

                 // 9. יחס רוחב/גובה מתאים למדבקת משלוח
                 double ratio = (double)box.Width / box.Height;
                 if (ratio < 0.3 || ratio > 5.5)
                     continue;

                 // 10. בדיקה שהאזור בפנים בהיר יותר מהרקע (אך לא דרישה קשיחה)
                 using var roi = new Mat(gray, box);
                 double mean = Cv2.Mean(roi).Val0;
                 if (mean < 90)  // גם מדבקה מלוכלכת עדיין בהירה יותר מרוב הרקע
                     continue;

                 result.Add(box);
             }

             return result;
         }
         static List<Rect> DetectLabels1(Mat src)
         {
             var result = new List<Rect>();

             using var gray = new Mat();
             using var blurred = new Mat();
             using var thresh1 = new Mat();
             using var thresh2 = new Mat();
             using var edges = new Mat();
             using var morph = new Mat();

             // 1. להמרה לגווני אפור
             Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);

             // 2. טשטוש לחיסול רעש
             Cv2.GaussianBlur(gray, blurred, new Size(7, 7), 0);

             // 3. סף אדפטיבי — להיות מאוד רגיש למדבקות לבנות
             Cv2.AdaptiveThreshold(
                 blurred, thresh1, 255,
                 AdaptiveThresholdTypes.GaussianC,
                 ThresholdTypes.BinaryInv,
                 41, 5);

             // 4. סף סטנדרטי נוסף (יותר גס) – יאתר מדבקות בוהקות
             Cv2.Threshold(blurred, thresh2, 180, 255, ThresholdTypes.BinaryInv);

             // 5. שילוב בין השניים
             Mat combined = new Mat();
             Cv2.BitwiseOr(thresh1, thresh2, combined);

             // 6. ניקוי כתמים + סגירה מורפולוגית
             Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(9, 9));
             Cv2.MorphologyEx(combined, morph, MorphTypes.Close, kernel, iterations: 2);

             // 7. קצוות כדי לזהות מסגרות של מדבקה
             Cv2.Canny(blurred, edges, 40, 120);

             // 8. חיבור הקצוות למפה הבינארית — נותן תוצאות הכי טובות
             Mat finalMask = new Mat();
             Cv2.BitwiseOr(morph, edges, finalMask);

             // 9. מציאת קונטורים
             Cv2.FindContours(finalMask, out Point[][] contours, out _,
                 RetrievalModes.External, ContourApproximationModes.ApproxSimple);

             double imgArea = src.Width * src.Height;

             foreach (var contour in contours)
             {
                 double area = Cv2.ContourArea(contour);

                 // כיוונון ספים יותר רחבים:
                 if (area < imgArea * 0.0015 || area > imgArea * 0.7)
                     continue;

                 var approx = Cv2.ApproxPolyDP(contour,
                     0.02 * Cv2.ArcLength(contour, true), true);

                 if (approx.Length < 4 || approx.Length > 12)
                     continue;

                 Rect r = Cv2.BoundingRect(approx);

                 double asp = (double)r.Width / r.Height;
                 if (asp < 0.35 || asp > 6.0)
                     continue;

                 // בדיקת בהירות — בודק ממוצע פיקסלים בתוך המלבן
                 using var roi = new Mat(gray, r);
                 double mean = Cv2.Mean(roi).Val0;
                 if (mean < 110) // הורדנו סף כדי לזהות מדבקות אפורות/מלוכלכות
                     continue;

                 result.Add(r);
             }

             return result;
         }

         /// <summary>
         /// מרחיב מלבן קצת מסביב למדבקה כדי שלא נחתוך צמוד מדי.
         /// </summary>
         static Rect ExpandRect1(Rect r, int imgW, int imgH, double percent)
         {
             int padX = (int)(r.Width * percent);
             int padY = (int)(r.Height * percent);

             int x = Math.Max(0, r.X - padX);
             int y = Math.Max(0, r.Y - padY);
             int w = Math.Min(imgW - x, r.Width + 2 * padX);
             int h = Math.Min(imgH - y, r.Height + 2 * padY);

             return new Rect(x, y, w, h);
         }
         static Rect ExpandRect2(Rect r, int imgW, int imgH, double percent)
         {
             int padX = (int)(r.Width * percent);
             int padY = (int)(r.Height * percent);

             int x = Math.Max(0, r.X - padX);
             int y = Math.Max(0, r.Y - padY);
             int w = Math.Min(imgW - x, r.Width + padX * 2);
             int h = Math.Min(imgH - y, r.Height + padY * 2);

             return new Rect(x, y, w, h);
         }

         static Mat DrawDebug(Mat src, List<Rect> boxes)
         {
             Mat dbg = src.Clone();

             foreach (var r in boxes)
             {
                 Cv2.Rectangle(dbg, r, Scalar.Red, 3);
             }

             return dbg;
         }

         static void DetectLabels(string imagePath, string outputPath)
         {
             // קריאת התמונה
             Mat image = Cv2.ImRead(imagePath);
             if (image.Empty())
             {
                 Console.WriteLine("שגיאה: לא ניתן לקרוא את התמונה");
                 return;
             }

             // המרה לגווני אפור
             Mat gray = new Mat();
             Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

             // החלת Gaussian Blur להפחתת רעש
             Mat blurred = new Mat();
             Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);

             // סף (Thresholding) לזיהוי אזורים בהירים (המדבקות הלבנות)
             Mat thresh = new Mat();
             Cv2.Threshold(blurred, thresh, 200, 255, ThresholdTypes.Binary);

             // מציאת קונטורים
             Point[][] contours;
             HierarchyIndex[] hierarchy;
             Cv2.FindContours(thresh, out contours, out hierarchy,
                 RetrievalModes.External, ContourApproximationModes.ApproxSimple);

             // סינון והצגת מדבקות
             List<LabelInfo> detectedLabels = new List<LabelInfo>();
             int labelCount = 0;

             foreach (var contour in contours)
             {
                 // חישוב שטח
                 double area = Cv2.ContourArea(contour);

                 // סינון לפי גודל מינימלי (התאם לפי הצורך)
                 if (area < 25000) continue;

                 // קבלת מלבן תוחם
                 Rect boundingRect = Cv2.BoundingRect(contour);

                 // סינון לפי יחס גובה-רוחב (מדבקות בדרך כלל מלבניות)
                 double aspectRatio = (double)boundingRect.Width / boundingRect.Height;

                 // רוב המדבקות הן רחבות יותר מאשר גבוהות או מרובעות
                 if (aspectRatio < 0.3 || aspectRatio > 4.0) continue;

                 labelCount++;
                 detectedLabels.Add(new LabelInfo
                 {
                     Id = labelCount,
                     BoundingBox = boundingRect,
                     Area = area,
                     AspectRatio = aspectRatio
                 });

                 // ציור מלבן סביב המדבקה
                 Cv2.Rectangle(image, boundingRect, new Scalar(0, 255, 0), 3);

                 // הוספת טקסט
                 string labelText = $"Label {labelCount}";
                 Cv2.PutText(image, labelText,
                     new Point(boundingRect.X, boundingRect.Y - 10),
                     HershapeFontTypes.HersheySimplex, 0.8, new Scalar(0, 255, 0), 2);
             }

             // הדפסת תוצאות
             Console.WriteLine($"\nנמצאו {detectedLabels.Count} מדבקות:\n");
             foreach (var label in detectedLabels)
             {
                 Console.WriteLine($"מדבקה {label.Id}:");
                 Console.WriteLine($"  מיקום: X={label.BoundingBox.X}, Y={label.BoundingBox.Y}");
                 Console.WriteLine($"  גודל: {label.BoundingBox.Width}x{label.BoundingBox.Height}");
                 Console.WriteLine($"  שטח: {label.Area:F0} פיקסלים");
                 Console.WriteLine($"  יחס גובה-רוחב: {label.AspectRatio:F2}");
                 Console.WriteLine();
             }

             // שמירת התמונה עם הזיהויים
            // string outputPath = "detected_labels.jpg";
             Cv2.ImWrite(outputPath, image);
             Console.WriteLine($"התמונה עם המדבקות המזוהות נשמרה ב: {outputPath}");

             // הצגת התמונה
             Cv2.ImShow("Detected Labels", image);
             Cv2.WaitKey(0);
             Cv2.DestroyAllWindows();

             // ניקוי זיכרון
             image.Dispose();
             gray.Dispose();
             blurred.Dispose();
             thresh.Dispose();
         }
     }

     class LabelInfo
     {
         public int Id { get; set; }
         public Rect BoundingBox { get; set; }
         public double Area { get; set; }
         public double AspectRatio { get; set; }
     }*/

    }

}


