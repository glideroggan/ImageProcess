using FaceRecognitionDotNet;

namespace api;

// TODO: this should probably use IFaceDetector
public static class MyFaceDetector
{
    private static FaceRecognition _service;

    static MyFaceDetector()
    {
        var basePath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        _service = FaceRecognition.Create(basePath + Path.DirectorySeparatorChar + "Models");
    }

    public static Image LoadImageFile(string path)
    {
        return FaceRecognition.LoadImageFile(path);
    }

    public static IEnumerable<Location> FaceLocations(Image image)
    {
        return _service.FaceLocations(image, 0, Model.Hog);
    }
}