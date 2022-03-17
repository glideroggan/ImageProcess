using contracts;
using FaceRecognitionDotNet;

namespace api;

// TODO: this should probably use IFaceDetector
public class MyFaceDetector : IFaceDetector
{
    private static FaceRecognition _service;

    // TODO: how do we set up the storage before starting to use it?
    private readonly IStorageProvider _storage;

    public MyFaceDetector(IStorageProvider storageProvider)
    {
        var basePath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        _service = FaceRecognition.Create(basePath + Path.DirectorySeparatorChar + "Models");
        _storage = storageProvider;
    }

    public static Image LoadImageFile(string path)
    {
        return FaceRecognition.LoadImageFile(path);
    }

    public static IEnumerable<Location> FaceLocations(Image image)
    {
        return _service.FaceLocations(image, 0);
    }

    public async Task<IEnumerable<Face>> FaceDetectAsync(string pathToImage)
    {
        using var unknownImage = LoadImageFile(pathToImage);
        var faceLocations = FaceLocations(unknownImage);
        var knownFaceLocation = faceLocations.ToList();
        if (!knownFaceLocation.Any())
        {
            return new List<Face>().AsEnumerable();
        }

        // TODO: don't forget to dispose the native encodings
        var faceEncodings = _service.FaceEncodings(unknownImage, knownFaceLocation, 0);
        var newFaces = faceEncodings.Select(x => new MyFaceEncoding
        {
            FaceId = Guid.NewGuid(),
            // how do we store these? array of double? another table that should be linked to the id?
            // we could try to store it in text
            FaceEncoding = x.GetRawEncoding()
        });
        var myFaceEncodings = newFaces.ToList();
        await _storage.SaveFaceEncodingAsync(myFaceEncodings);
        return myFaceEncodings.Select(x => new Face
        {
            Id = x.FaceId
        });
    }

    public Task<List<FaceVerify>> FaceVerifyAsync(Guid face1, Dictionary<string, List<Guid>> faces)
    {
        // https://github.com/takuya-takeuchi/DlibDotNet/blob/develop/examples/DnnFaceRecognition/Program.cs
        throw new NotImplementedException();
    }
}