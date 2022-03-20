namespace contracts;

public interface IFacePlugin
{
    // TODO: we should set the TTL of the faces already here, so that we can set the expire timer in storage
    Task<IEnumerable<Face>> FaceDetectAsync(string pathToImage);
    ValueTask<List<FaceVerify>> FaceVerifyAsync(Face face1, Dictionary<string, Person> faces);
    string Identifier { get; }
    Task<Face> GetAttributesAsync(Stream stream);
}

public interface IFaceDetector
{
    Task<List<FaceVerify>> FaceVerifyAsync(Face faceToIdentify, string systemId);
    Task<List<Face>> FaceDetectAsync(string profilePathToImage, string name, TimeSpan expireTtl);
    Task<List<Face>> FaceDetectAsync(string profilePathToImage);
    Task<Face> GetAttributesAsync(AttributeEnum attribute, Stream imageStream);
}

public interface IStorageProvider
{
    Task AddFacesAsync(string name, DateOnly expireDate, string systemId, byte[]? blob = null, params Face[] faces);
    Task<Dictionary<string, Person>> GetKnownFacesAsync(string systemId);
    Task SaveFaceEncodingAsync(double[] encoding, Guid faceId);
}

public enum AttributeEnum
{
    All,
    Age
}

public class Emotion
{
    public double Anger { get; set; }
    public double Contempt { get; set; }
    public double Disgust { get; set; }
    public double Fear { get; set; }
    public double Happiness { get; set; }
    public double Neutral { get; set; }
    public double Sadness { get; set; }
    public double Surprise { get; set; }
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
    public string Person { get; set; }
    public double Confidence { get; set; }
    public bool IsIdentical { get; set; }
}

public class Person
{
    public string Name { get; set; }
    public List<Face> Faces { get; set; }
}

public class FaceAttribute
{
    
}

public class Face
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string SystemId { get; set; }
    public double[]? Encoding { get; set; }
    public double? Age { get; set; }
    public Emotion Emotions { get; set; }
}
