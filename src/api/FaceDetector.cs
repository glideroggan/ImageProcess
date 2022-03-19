using contracts;

namespace api;

public class FaceDetector : IFaceDetector
{
    private readonly IFacePlugin[] _plugins;

    private readonly IStorageProvider _storage;
    // TODO: lets have the plugins in here and also storage, so each plugin will not bother with storage
    // and this wrapper will also deal with the different attributes that different plugins support

    public FaceDetector(IEnumerable<IFacePlugin> plugins, IStorageProvider storage)
    {
        _plugins = plugins.ToArray();
        _storage = storage;
    }
    public async Task<List<FaceVerify>> FaceVerifyAsync(Face faceToIdentify, string systemId)
    {
        // verify a face only works if the face have been detected within the same system
        // so we should use the same detector that was used to find the face, OR do the whole chain again
        // if we need feature that just that detector have
        var detector = _plugins.FirstOrDefault(x => x.Identifier.Equals(systemId));
        // get faces from storage
        var storedFaces = await _storage.GetKnownFacesAsync(systemId);
        return await detector.FaceVerifyAsync(faceToIdentify, storedFaces);
    }

    /// <summary>
    /// Detect faces in the image and store away under "name"
    /// </summary>
    /// <param name="pathToImage"></param>
    /// <param name="name"></param>
    /// <param name="expireTtl"></param>
    /// <returns></returns>
    public async Task<List<Face>> FaceDetectAsync(string pathToImage, string name, TimeSpan expireTtl)
    {
        var (faceList, identifier) = await DetectFaceInternal(pathToImage);
        
        // save to storage
        await SaveToStorage(name, expireTtl, identifier, faceList);

        return faceList?.ToList();
    }
    /// <summary>
    /// Detect faces in the image
    /// No storage will be used
    /// </summary>
    /// <param name="profilePathToImage"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<List<Face>> FaceDetectAsync(string pathToImage)
    {
        var (faceList, _) = await DetectFaceInternal(pathToImage);

        return faceList?.ToList();
    }

    private async Task<(List<Face>, string)> DetectFaceInternal(string pathToImage)
    {
        // TODO: what to do when more than one face is found?
        IEnumerable<Face>? detectedFaces = null;
        var identifier = string.Empty;
        
        foreach (var detector in _plugins)
        {
            detectedFaces = await detector.FaceDetectAsync(pathToImage);
            if (!detectedFaces.Any()) continue;

            identifier = detector.Identifier;
            break;
        }

        var faceList = detectedFaces.ToList();

        if (!faceList.Any())
        {
            throw new Exception("No faces found");
        }

        return (faceList, identifier);
    }

    private async Task SaveToStorage(string name, TimeSpan expireTtl, string identifier, List<Face> faceList)
    {
        await _storage.AddFacesAsync(name,
            DateOnly.FromDateTime(DateTime.UtcNow + expireTtl),
            identifier,
            null,
            faceList.ToArray());
        await _storage.SaveFaceEncodingAsync(faceList.Single().Encoding, faceList.Single().Id);
    }

    
}