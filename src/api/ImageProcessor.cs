using System.Buffers;
using System.IO.Pipelines;
using contracts;

namespace api
{
    public class Profile
    {
        public Profile(string filename, string? name)
        {
            PathToImage = filename;
            Name = name;
        }

        public string PathToImage { get; }
        public string? Name { get; set; }
    }

    public class ApiResults<T>
    {
        public T? Data { get; set; }
        public ApiError? Error { get; set; }
    }

    public class ApiError
    {
        public ApiError(int statusCode, string message)
        {
            StatusCode = statusCode;
            Message = message;
        }

        public int StatusCode { get; init; }
        public string Message { get; init; }
    }

    // TODO: should move this over to a classlib also
    public class ImageProcessor
    {
        private int _idCounter;
        private readonly IFaceDetector _faceDetector;

        public ImageProcessor(IFaceDetector faceDetector)
        {
            _faceDetector = faceDetector;
        }

        internal async Task<ApiResults<bool>> AddNewFace(HttpContext context, string name)
        {
            // PERF: use an array pool here instead
            // TODO: we should precount the counter based on the images in the folder already
            var filename = $"upload{_idCounter}.jpg";
            var profile = await ReadDataFromRequestAndWriteToFileAsync(context, filename, name);

            var results = new ApiResults<bool>();
            // TODO: make this value configurable from the config
            var defaultTtlFace = TimeSpan.FromHours(24);
            var faces = await _faceDetector.FaceDetectAsync(profile.PathToImage, name, defaultTtlFace);
            if (!faces.Any())
            {
                return new ApiResults<bool>
                {
                    Error = new ApiError(400, "No face found!")
                };
            }

            results.Data = true;

            // store the faceIds
            // TODO: add the image that belongs with the id
            // await _storageProvider.AddFacesAsync(name, DateOnly.FromDateTime(DateTime.UtcNow.Add(defaultTtlFace)),
            //     _faceDetector.Identifier,null,faces.First());

            return results;
        }


        internal async Task<ApiResults<FaceMatch>> VerifyFace(HttpContext context)
        {
            // TODO: this call could take time, so async could be better
            // TODO: for async calls, get it from requests and write to file and add to db
            // add to queue?

            // PERF: use an array pool here instead
            var filename = $"image{_idCounter}.jpg";
            // PERF: don't save to file, if not async, use the stream directly
            var profile = await ReadDataFromRequestAndWriteToFileAsync(context, filename, null);

            // TODO: cut image to only get one face
            // https://docs.microsoft.com/en-us/dotnet/api/system.drawing.bitmap?view=dotnet-plat-ext-6.0
            // using var bitmap = unknownImage.ToBitmap();


            var faces = (await _faceDetector.FaceDetectAsync(profile.PathToImage)).ToList();
            if (!faces.Any())
            {
                return new ApiResults<FaceMatch>
                {
                    Error = new ApiError(400, "No face found!")
                };
            }

            if (faces.Count > 1)
            {
                return new ApiResults<FaceMatch>
                {
                    Error = new ApiError(401, "Too many faces!")
                };
            }

            var verifyResults = await _faceDetector.FaceVerifyAsync(
                faces.Single(), faces.Single().SystemId);

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


        private async Task<Profile> ReadDataFromRequestAndWriteToFileAsync(HttpContext context, string filename,
            string? name = default)
        {
            var reader = context.Request.BodyReader;

            var path = $"processFolder/{filename}";
            var profileResults = new Profile(path, name);
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

        public async Task GetAttributes(HttpContext ctx, Guid faceId = default)
        {
            // TODO: check with azure which kind of attributes it have

            // check which system the faceId belongs to
            // var (systemId) = _storageProvider.GetFaceAsync(faceId);

            // get that feature from the data


            throw new NotImplementedException();
        }

        public async Task GetFaces(HttpContext context, Guid faceId = default)
        {
            // TODO: here we might actually want to just use the storage?
            // TODO: return all stored faces OR just one faceId if used
            // await _storageProvider.GetKnownFacesAsync();

            throw new NotImplementedException();
        }
    }
}