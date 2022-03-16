using contracts;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace azure_face;

public class AzureFaceServicesOptions
{
    public const string AzureFaceServices = "AzureFaceServices";
    public string ApiKey { get; set; }
    public string Endpoint { get; set; }
}

public class AzureFaceServices : IFaceDetector
{
    private static string _subscriptionKey = "";
    private static string _endpoint = "";

    public AzureFaceServices(string apiKey, string endpoint)
    {
        _subscriptionKey = apiKey;
        _endpoint = endpoint;
    }
    public async Task<IEnumerable<Face>> FaceDetectAsync(string pathToImage)
    {
        var client = new FaceClient(new ApiKeyServiceClientCredentials(_subscriptionKey)) { Endpoint = _endpoint };
        using var fileStream = File.Open(pathToImage, FileMode.Open, FileAccess.Read);
        var detectedFaces = await client.Face.DetectWithStreamAsync(fileStream);
        return detectedFaces.Select(detectedFace => new Face { Id = detectedFace.FaceId.Value }).ToList();
    }

    public async Task<List<FaceVerify>> FaceVerifyAsync(Guid face1, Dictionary<string, List<Guid>> faces)
    {
        var client = new FaceClient(new ApiKeyServiceClientCredentials(_subscriptionKey)) { Endpoint = _endpoint };
        // using var fileStream = File.Open(pathToImage, FileMode.Open, FileAccess.Read);
        // var detectedFaces = await client.Face.DetectWithStreamAsync(fileStream);
        // if ()
        // TODO: for now run the api for each faceId, but later we should send in ONE faceId and it will match it
        // with an already stored faceId, and we can check in db who that is
        var results = new List<FaceVerify>(); 
        foreach (var person in faces)
        {
            foreach (var faceId in person.Value)
            {
                var res = await client.Face.VerifyFaceToFaceAsync(face1, faceId);
                results.Add(new FaceVerify { Person = person.Key, Confidence = res.Confidence, IsIdentical = res.IsIdentical});
            }
        }

        return results;
    }
}
