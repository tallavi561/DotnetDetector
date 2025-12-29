using StickersDetector.Models;
using StickersDetector.bl.OpenCV;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing; 
var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Map controllers
app.MapControllers();


var definitions = new List<InputLabelDefinition>
{
    new InputLabelDefinition("DHL", "labels/DHL.jpg") // ציון השם והנתיב לקובץ
};
// 2️⃣ יצירת הדטקטור (אתחול כבד קורה כאן)
using var detector = new LabelDetector(definitions);

// 3️⃣ טעינת תמונת הקלט
var imagePath = "example-pictures/3.jpg";
using var image = CvInvoke.Imread(imagePath);

if (image.IsEmpty)
{
      Console.WriteLine($"Failed to load image: {imagePath}");
      return;
}

// 4️⃣ זיהוי המדבקה "A"
var detection = detector.Detect(image, "DHL");

// 5️⃣ טיפול בתוצאה
if (detection == null)
{
      Console.WriteLine("Label A was NOT detected.");
      return;
}

Console.WriteLine("Label A detected!");
Console.WriteLine($"Confidence: {detection.Confidence:P1}");
Console.WriteLine($"Center: {detection.Center}");
Console.WriteLine($"Corners:");

foreach (var corner in detection.Corners)
{
      Console.WriteLine($"  {corner}");
}

if (detection != null)
{
    // 1. המרת הפינות מ-Point2D ל-System.Drawing.Point עבור Emgu.CV
    var points = detection.Corners
        .Select(p => new System.Drawing.Point(p.X, p.Y))
        .ToArray();

    // 2. ציור המרובע על התמונה המקורית (בצבע ירוק, עובי 3)
    // הערה: נשתמש ב-true כדי לסגור את הצורה בין הנקודה האחרונה לראשונה
    CvInvoke.Polylines(image, points, true, new MCvScalar(0, 255, 0), 3);

    // 3. שמירת התמונה עם הסימון לדיסק (לצורך בדיקה ב-WSL)
    string markedPath = "example-pictures/marked_result.jpg";
    CvInvoke.Imwrite(markedPath, image);
    Console.WriteLine($"Marked image saved to: {markedPath}");
}



app.Run();
