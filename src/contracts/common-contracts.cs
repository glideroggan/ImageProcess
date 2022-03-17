namespace contracts;

public interface IFaceDetector
{
    // TODO: we should set the TTL of the faces already here, so that we can set the expire timer in storage
    Task<IEnumerable<Face>> FaceDetectAsync(string pathToImage);
    Task<List<FaceVerify>> FaceVerifyAsync(Guid face1, Dictionary<string, List<Guid>> faces);
}

public interface IStorageProvider
{
    Task AddFacesAsync(string name, DateOnly expireDate, byte[]? blob = null, params Face[] faces);
    Task<Dictionary<string, List<Guid>>> GetKnownFacesAsync();
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
    public string Person { get; set; }
    public double Confidence { get; set; }
    public bool IsIdentical { get; set; }
}
public class Face
{
    public Guid Id { get; set; }
}
