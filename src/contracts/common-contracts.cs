using System.Drawing;

namespace contracts;

public interface IFaceDetector
{
    // TODO: we should set the TTL of the faces already here, so that we can set the expire timer in storage
    Task<IEnumerable<Face>> FaceDetectAsync(string pathToImage);
    ValueTask<List<FaceVerify>> FaceVerifyAsync(Face face1, Dictionary<string, Person> faces);
}

public interface IStorageProvider
{
    Task AddFacesAsync(string name, DateOnly expireDate, byte[]? blob = null, params Face[] faces);
    Task<Dictionary<string, Person>> GetKnownFacesAsync();
    Task SaveFaceEncodingAsync(IEnumerable<MyFaceEncoding> newFaces);
}

public class MyFaceEncoding
{
    public Guid FaceId { get; set; }
    public double[] FaceEncoding { get; set; }
}

public class FaceMatch
{
    public bool Identified { get; set; }
    public string? Name { get; set; }
}

public class FaceVerify
{
    public FaceVerify(double confidence, string person)
    {
        Person = person;
        Confidence = confidence;
        IsIdentical = confidence < .9;
    }
    public string Person { get; init; }
    public double Confidence { get; init; }
    public bool IsIdentical { get; }
}

public class Person
{
    public string Name { get; set; }
    public List<Face> Faces { get; set; }
}

public class Face
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public double[]? Encoding { get; set; }
    public Rectangle Location { get; set; }
}
