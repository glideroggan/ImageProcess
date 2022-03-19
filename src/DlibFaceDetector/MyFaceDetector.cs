﻿using contracts;
using FaceRecognitionDotNet;

namespace DlibFaceDetector;

public class MyFaceDetector : IFaceDetector
{
    private readonly FaceRecognition _service;

    // TODO: how do we set up the storage before starting to use it?
    private readonly IStorageProvider _storage;

    public MyFaceDetector(IStorageProvider storageProvider)
    {
        var basePath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName);
        _service = FaceRecognition.Create(basePath + Path.DirectorySeparatorChar + "Models");
        _storage = storageProvider;
    }

    public static Image LoadImageFile(string path)
    {
        return FaceRecognition.LoadImageFile(path);
    }

    public IEnumerable<Location> FaceLocations(Image image)
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
            Id = x.FaceId,
            Encoding = x.FaceEncoding,
        });
    }

    public ValueTask<List<FaceVerify>> FaceVerifyAsync(Face face, Dictionary<string, Person> people)
    {
        // https://github.com/takuya-takeuchi/DlibDotNet/blob/develop/examples/DnnFaceRecognition/Program.cs
        var res = new List<FaceVerify>();
        // TODO: compare face1 with the list of faceIds and those that are the nearest should be more similar
        // var t = faceIds.Select(x => new FaceEncoding())
        // PERF: use the FaceDistances, to compare several at once
        foreach (var person in people.Values)
        {
            foreach (var knownFace in person.Faces)
            {
                var face1 = FaceRecognition.LoadFaceEncoding(face.Encoding);
                if (knownFace.Encoding == null) continue;   // BUG: if we get encodings from azure, we should have the encodings here
                var face2 = FaceRecognition.LoadFaceEncoding(knownFace.Encoding);
                var dist = FaceRecognition.FaceDistance(face1, face2);
                if (dist < .6)
                {
                    res.Add(new FaceVerify
                    {
                        Confidence = dist,
                        Person = person.Name,
                        IsIdentical = dist < 0.9
                    });
                }
            }
        }

        return new ValueTask<List<FaceVerify>>(res);
        
    }
}