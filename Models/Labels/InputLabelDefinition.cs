namespace StickersDetector.Models
{
      public class InputLabelDefinition
      {
            public string Name { get; }
            public string ImagePath { get; }

            public InputLabelDefinition(string name, string imagePath)
            {
                  Name = name;
                  ImagePath = imagePath;
            }
      }

}
