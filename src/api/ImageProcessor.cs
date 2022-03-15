using System.Buffers;
using System.IO.Pipelines;
using contracts;

namespace api
{
    public class Profile
    {
        public Profile(int id, string filename)
        {
            PathToImage = filename;
        }

        public string PathToImage { get; private set; }
        public string Name { get; set; }
    }

    public class ApiResult
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }
    }
    public class ImageProcessor
    {
        private int idCounter;
        private readonly IFaceDetector _faceDetector;
        private readonly IStorageProvider _storageProvider;

        public ImageProcessor(IFaceDetector faceDetector, IStorageProvider storageProvider)
        {
            _faceDetector = faceDetector;
            _storageProvider = storageProvider;
        }

        internal async Task AddNewFace(HttpContext context, string name)
        {
            // PERF: use an array pool here instead
            // TODO: we should precount the counter based on the images in the folder already
            var filename = $"upload{idCounter}.jpg";
            var profile = await ReadDataFromRequestAndWriteToFileAsync(context, filename);
            profile.Name = name;
            
            // TODO: do a face detect to make sure that there is a face in the image
            var faces = await _faceDetector.FaceDetectAsync(profile.PathToImage);
            if (!faces.Any())
            {
                // TODO: add reason
                context.Response.StatusCode = 400;
            }
            // store the faceIds
            // TODO: we should add the TTL value to db
            await _storageProvider.AddFacesAsync(name, faces.First());
        }

        internal async Task Process(HttpContext context)
        {
            // PERF: use an array pool here instead
            var filename = $"image{idCounter}.jpg";
            var profile = await ReadDataFromRequestAndWriteToFileAsync(context, filename);
            
            // get face locations
            // using var unknownImage = MyFaceDetector.LoadImageFile(profile.PathToImage);
            // var faceLocations = MyFaceDetector.FaceLocations(unknownImage);
            
            // TODO: cut image to only get one face
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.bitmap?view=dotnet-plat-ext-6.0
            // using var bitmap = unknownImage.ToBitmap();
            
            // TODO: send to azure?
            var faces = await _faceDetector.FaceDetectAsync(profile.PathToImage);
            if (!faces.Any())
            {
                // TODO: add reason
                context.Response.StatusCode = 400;
                return;
            }

            var faceIds = await _storageProvider.GetKnownFacesAsync();
            
            var verifyResults = await _faceDetector.FaceVerifyAsync(faces.First().Id, faceIds);
        }

        private async Task<Profile> ReadDataFromRequestAndWriteToFileAsync(HttpContext context, string filename)
        {
            var reader = context.Request.BodyReader;

            var path = $"processFolder/{filename}";
            var profileResults = new Profile(idCounter++, path);
            using var fileStream = File.Create(path);
            var writer = PipeWriter.Create(fileStream);
            try
            {
                while (true)
                {
                    var readResult = await reader.ReadAsync();
                    var buffer = readResult.Buffer;

                    var dataToWrite = new byte[buffer.Length];
                    buffer.CopyTo(dataToWrite);
                    await writer.WriteAsync(dataToWrite);

                    if (readResult.IsCompleted) break;

                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            finally
            {
                await reader.CompleteAsync();
                // await fileStream.FlushAsync();
                await writer.CompleteAsync();
            }

            return profileResults;
        }
    }
}
