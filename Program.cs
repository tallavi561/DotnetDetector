using StickersDetector.Models;
using StickersDetector.bl.OpenCV;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;


var builder = WebApplication.CreateBuilder(args);

// 1. הגדרת רשימת המדבקות כפי שמופיע בהערות שלך
var definitions = new List<InputLabelDefinition>
{
    new InputLabelDefinition("DHL", "labels/DHL.jpg") 
};

// 2. רישום ה-LabelDetector כ-Singleton
// השתמשתי ב-Singleton כי ציינת שזה "אתחול כבד" - כך זה יקרה רק פעם אחת בהרצת האפליקציה
builder.Services.AddSingleton(new LabelDetector(definitions));

// הוספת תמיכה בקונטרולרים
builder.Services.AddControllers();

var app = builder.Build();

// הגדרת ניתוב הקונטרולרים
app.MapControllers();

app.Run();


// var builder = WebApplication.CreateBuilder(args);

// // Add controllers
// builder.Services.AddControllers();

// var app = builder.Build();

// // Map controllers
// app.MapControllers();


// app.Run();



// var definitions = new List<InputLabelDefinition>
// {
//     new InputLabelDefinition("DHL", "labels/DHL.jpg") // ציון השם והנתיב לקובץ
// };
// // 2️⃣ יצירת הדטקטור (אתחול כבד קורה כאן)
// using var detector = new LabelDetector(definitions);

// // 3️⃣ טעינת תמונת הקלט
// var imagePath = "example-pictures/3.jpg";
// using var image = CvInvoke.Imread(imagePath);

// if (image.IsEmpty)
// {
//       Console.WriteLine($"Failed to load image: {imagePath}");
//       return;
// }

// // 4️⃣ זיהוי המדבקה "A"
// var detection = detector.Detect(image, "DHL");

// // 5️⃣ טיפול בתוצאה
// if (detection == null)
// {
//       Console.WriteLine("Label A was NOT detected.");
//       return;
// }

// Console.WriteLine("Label A detected!");
// Console.WriteLine($"Confidence: {detection.Confidence:P1}");
// Console.WriteLine($"Center: {detection.Center}");
// Console.WriteLine($"Corners:");

// foreach (var corner in detection.Corners)
// {
//       Console.WriteLine($"  {corner}");
// }

// if (detection != null)
// {
//       var points = detection.Corners
//           .Select(p => new System.Drawing.Point(p.X, p.Y))
//           .ToArray();


//       CvInvoke.Polylines(image, points, true, new MCvScalar(0, 255, 0), 3);

//       string markedPath = "example-pictures/marked_result.jpg";
//       CvInvoke.Imwrite(markedPath, image);
//       Console.WriteLine($"Label {detection.LabelName} found. Extracting...");

//       using var finalLabel = ImageRotator.ExtractAndAlignLabel(image, detection.Corners);

//       string finalPath = "example-pictures/final_crop.jpg";
//       CvInvoke.Imwrite(finalPath, finalLabel);

//       Console.WriteLine($"Aligned and cropped label saved to: {finalPath}");
// }