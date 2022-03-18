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

    public class ApiResults<T>
    {
        public T? Data { get; set; }
        public ApiError? Error { get; set; }
    }

    public class ApiError
    {
        public int StatusCode { get; set; }
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

        internal async Task<ApiResults<bool>> AddNewFace(HttpContext context, string name)
        {
            // PERF: use an array pool here instead
            // TODO: we should precount the counter based on the images in the folder already
            var filename = $"upload{idCounter}.jpg";
            var profile = await ReadDataFromRequestAndWriteToFileAsync(context, filename);
            profile.Name = name;

            var results = new ApiResults<bool>();
            var defaultTtlFace = TimeSpan.FromHours(24);
            var faces = await _faceDetector.FaceDetectAsync(profile.PathToImage);
            if (!faces.Any())
            {
                return new ApiResults<bool>
                {
                    Error = new ApiError { StatusCode = 400, Message = "No face found!" }
                };
            }

            results.Data = true;

            // store the faceIds
            // TODO: add the image that belongs with the id
            await _storageProvider.AddFacesAsync(name, DateOnly.FromDateTime(DateTime.UtcNow.Add(defaultTtlFace)), null,
                faces.First());

            return results;
        }
        
        

        internal async Task<ApiResults<FaceMatch>> VerifyFace(HttpContext context, bool? async = default)
        {
            // TODO: this call could take time, so async could be better
            // TODO: for async calls, get it from requests and write to file and add to db
            // add to queue?

            // PERF: use an array pool here instead
            var filename = $"image{idCounter}.jpg";
            // PERF: don't save to file, if not async, use the stream directly
            var profile = await ReadDataFromRequestAndWriteToFileAsync(context, filename);

            // TODO: cut image to only get one face
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.bitmap?view=dotnet-plat-ext-6.0
            // using var bitmap = unknownImage.ToBitmap();
            
            var faces = (await _faceDetector.FaceDetectAsync(profile.PathToImage)).ToList();
            if (!faces.Any())
            {
                return new ApiResults<FaceMatch>
                {
                    Error = new ApiError { StatusCode = 400, Message = "No face found!" }
                };
            }
            if (faces.Count > 1)
            {
                return new ApiResults<FaceMatch>
                {
                    Error = new ApiError { StatusCode = 401, Message = "Too many faces!" }
                };
            }

            var faceIds = await _storageProvider.GetKnownFacesAsync();
            var verifyResults = await _faceDetector.FaceVerifyAsync(faces.Single(), faceIds);

            var res = new FaceMatch
            {
                Identified = verifyResults.Any(x => x.IsIdentical),
                Name = verifyResults.Where(x => x.IsIdentical)
                    .Select(x => x.Person)
                    .FirstOrDefault()
            };
            // TODO: if no identity found, but a face was found, send back face coordinates at least?

            return new ApiResults<FaceMatch>()
            {
                Data = res
            };
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

                    reader.AdvanceTo(buffer.End);
                }
            }
            finally
            {
                await writer.FlushAsync();
                await reader.CompleteAsync();
                await writer.CompleteAsync();
            }

            return profileResults;
        }

        public async Task GetAttributes(HttpContext ctx, Guid faceId)
        {
            // TODO: check with azure which kind of attributes it have
            
            // check which system the faceId belongs to
            // var (systemId) = _storageProvider.GetFaceAsync(faceId);
            
            // get that feature from the data
            
            
            throw new NotImplementedException();
        }

        public async Task GetFaces(HttpContext context)
        {
            // TODO: return all stored faces
            await _storageProvider.GetKnownFacesAsync();
            
            throw new NotImplementedException();
        }
    }
}