using System.Diagnostics;
using contracts;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Emotion = contracts.Emotion;
using Person = contracts.Person;

namespace azure_face;

/*
 * TODO:
 *  - can we get the face encoding values of Azure?
 */

public class AzureFaceServicesOptions
{
    public const string AzureFaceServices = "AzureFaceServices";
    public string ApiKey { get; set; }
    public string Endpoint { get; set; }
}

public class AzureFaceServices : IFacePlugin
{
    private static string _subscriptionKey = "";
    private static string _endpoint = "";
    public string Identifier => "Azure";

    public AzureFaceServices(string apiKey, string endpoint)
    {
        _subscriptionKey = apiKey;
        _endpoint = endpoint;
    }

    public async Task<IEnumerable<Face>> FaceDetectAsync(string pathToImage)
    {
        using var client = new FaceClient(new ApiKeyServiceClientCredentials(_subscriptionKey))
            { Endpoint = _endpoint };
        using var fileStream = File.Open(pathToImage, FileMode.Open, FileAccess.Read);
        var detectedFaces = await client.Face.DetectWithStreamAsync(fileStream);
        return detectedFaces.Select(detectedFace => new Face { Id = detectedFace.FaceId.Value, SystemId = Identifier })
            .ToList();
    }

    public async Task<Face> GetAttributesAsync(Stream stream)
    {
        var allAttributes = new List<FaceAttributeType>
        {
            // FaceAttributeType.Mask, FaceAttributeType.QualityForRecognition
            
            FaceAttributeType.Accessories, FaceAttributeType.Age, FaceAttributeType.Blur, FaceAttributeType.Emotion,
            FaceAttributeType.Exposure, FaceAttributeType.Gender, FaceAttributeType.Glasses, FaceAttributeType.Hair,
            FaceAttributeType.Makeup,
            FaceAttributeType.Noise, FaceAttributeType.Occlusion, FaceAttributeType.Smile,
            FaceAttributeType.FacialHair, FaceAttributeType.HeadPose
        };
        using var client = new FaceClient(new ApiKeyServiceClientCredentials(_subscriptionKey))
            { Endpoint = _endpoint };
        // using var newStream = new MemoryStream((int)stream.Length);
        // await stream.CopyToAsync(newStream);
        // var pos = newStream.Position;
        stream.Position = 0;
        var detectedFaces = await client.Face.DetectWithStreamAsync(stream, true, 
            true, allAttributes);
        // TODO: We should ofc handle more than one face
        Debug.Assert(detectedFaces.Count == 1);
        var azureFace = detectedFaces.First();
        var face = detectedFaces.Select(detectedFace => new Face 
                { Id = detectedFace.FaceId.Value, SystemId = Identifier  })
            .First();
        face.Age = azureFace.FaceAttributes.Age;
        face.Emotions = new Emotion
        {
            Anger = azureFace.FaceAttributes.Emotion.Anger,
            Contempt = azureFace.FaceAttributes.Emotion.Contempt,
            Disgust = azureFace.FaceAttributes.Emotion.Disgust,
            Fear = azureFace.FaceAttributes.Emotion.Fear,
            Happiness = azureFace.FaceAttributes.Emotion.Happiness,
            Neutral = azureFace.FaceAttributes.Emotion.Neutral,
            Sadness = azureFace.FaceAttributes.Emotion.Sadness,
            Surprise = azureFace.FaceAttributes.Emotion.Surprise
        };
        return face;
    }

    public async ValueTask<List<FaceVerify>> FaceVerifyAsync(Face face1, Dictionary<string, Person> faces)
    {
        var client = new FaceClient(new ApiKeyServiceClientCredentials(_subscriptionKey)) { Endpoint = _endpoint };
        // using var fileStream = File.Open(pathToImage, FileMode.Open, FileAccess.Read);
        // var detectedFaces = await client.Face.DetectWithStreamAsync(fileStream);
        // if ()
        // TODO: for now run the api for each faceId, but later we should send in ONE faceId and it will match it
        // with an already stored faceId, and we can check in db who that is
        var results = new List<FaceVerify>();
        foreach (var person in faces.Values)
        {
            foreach (var face in person.Faces)
            {
                // TODO: we need to add SystemId, as the faceIds can't be mixed
                // between the different detectors
                if (face.Encoding != null) continue;
                var res = await client.Face.VerifyFaceToFaceAsync(face.Id, face.Id);
                results.Add(new FaceVerify
                    { Person = person.Name, Confidence = res.Confidence, IsIdentical = res.IsIdentical });
            }
        }

        return results;
    }
}