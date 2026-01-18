using StickersDetector.Models;
using StickersDetector.bl.OpenCV;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;


var builder = WebApplication.CreateBuilder(args);

// 1. הגדרת רשימת המדבקות כפי שמופיע בהערות שלך
var definitions = new List<InputLabelDefinition>
{
    new InputLabelDefinition("DHL", "labels/DHL.jpg"),
    new InputLabelDefinition("FED-EX", "labels/FED-EX.jpg"),
    new InputLabelDefinition("IsraelPostOffice", "labels/IsraelPostOffice.jpg")
};

// 2. רישום ה-LabelDetector כ-Singleton
// השתמשתי ב-Singleton כי ציינת שזה "אתחול כבד" - כך זה יקרה רק פעם אחת בהרצת האפליקציה
builder.Services.AddSingleton(new LabelDetector(definitions));


// TEST
var imagesBasePaths = "example-pictures/";
var imagesPaths = new List<string>
{
    "DHL", "FED-EX", "IsraelPostOffice"
};
var imagesName = "1.jpg";
var allImagesPaths = imagesPaths
    .Select(name => imagesBasePaths + name + "/" + imagesName)
    .ToList();


// var detection = detector.Detect(image, "DHL");
using var detector = new LabelDetector(definitions);
for (int i = 0; i < allImagesPaths.Count; i++)
{
    var imagesPath = allImagesPaths[i];

    using var image = CvInvoke.Imread(imagesPath);
    if (image.IsEmpty)
    {
        Console.WriteLine($"Failed to load image: {imagesPath}");
        continue;
    }
    // get the time before detection
    var startTime = DateTime.Now;
    var comparedLabel = imagesPaths[i];
    var detection = detector.Detect(image, comparedLabel);
    var endTime = DateTime.Now;
    Console.WriteLine($"Detection time for {comparedLabel}: {(endTime - startTime).TotalMilliseconds} ms");
}





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

// if (image.IsEmpty)
// {
//       Console.WriteLine($"Failed to load image: {imagePath}");
//       return;
// }

// // 4️⃣ זיהוי המדבקה "A"

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