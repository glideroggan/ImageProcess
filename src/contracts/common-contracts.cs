namespace contracts;

public interface IFaceDetector
{
    Task<IEnumerable<Face>> FaceDetectAsync(string pathToImage);
    Task<List<FaceVerify>> FaceVerifyAsync(Guid face1, Dictionary<string, List<Guid>> faces);
}

public interface IStorageProvider
{
    Task AddFacesAsync(string name, params Face[] faces);
    Task<Dictionary<string, List<Guid>>> GetKnownFacesAsync();
}

public class FaceMatch
{
    public string Name { get; set; }
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
