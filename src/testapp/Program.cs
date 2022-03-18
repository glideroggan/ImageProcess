using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.Json;
using contracts;
using DlibFaceDetector;
using FaceRecognitionDotNet;

/* TODO:
 *  program.exe -i|images path|filepath
 *      -f|features [<feature1>,<feature2>] 
 *  - a dotnet tool
 *  - add face locations to new image
 * 
 */

var flagBuilder = new FlagParser.Builder<FlagOptions>()
    .AddFlag("images", 'i', (val, c) =>
    {
        c.Images = val.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    })
    .AddFlag("copy", 'c', (s, flagOptions) =>
    {
        flagOptions.Copy = bool.Parse(s);
    });
var options = flagBuilder.Parse(args);


var state = new State();

// input
if (options!.Images.Length > 0)
{
    // read in images and send through dlibdotnet
    var detector = new MyFaceDetector(null);
    foreach (var imagePath in options.Images)
    {
        var faces = await detector.FaceDetectAsync(imagePath);
        state.Faces.AddRange(faces);
    }
}

// output

if (options.Output == null)
{
    Console.Out.WriteLineAsync(JsonSerializer.Serialize(state));
}

if (options.Copy && options.Images.Length > 0)
{
    // TODO: output copies of the images, with the attributes
    using var pen = new Pen(Color.Red, 5);
    foreach (var image in options.Images)
    {
        var rect = state.Faces.First().Location;
        using var image2 = FaceRecognition.LoadImageFile(image);
        using var bitmap = image2.ToBitmap();
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.DrawRectangle(pen, rect);
        bitmap.Save($"{Path.GetFileName(image)}_copy{Path.GetExtension(image)}");
    }
    
}

internal class State
{
    public List<Face> Faces { get; set; } = new();
}

internal class FlagOptions
{
    internal string[] Images;
    public object Output { get; set; }
    public bool Copy { get; set; }
}