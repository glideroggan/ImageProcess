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

        internal async Task AddNewFace(HttpContext context, string name)
        {
            // PERF: use an array pool here instead
            // TODO: we should precount the counter based on the images in the folder already
            var filename = $"upload{idCounter}.jpg";
            var profile = await ReadDataFromRequestAndWriteToFileAsync(context, filename);
            profile.Name = name;

            var defaultTtlFace = TimeSpan.FromHours(24);
            var faces = await _faceDetector.FaceDetectAsync(profile.PathToImage);
            if (!faces.Any())
            {
                // TODO: add reason
                context.Response.StatusCode = 400;
                return;
            }

            // store the faceIds
            // TODO: add the image that belongs with the id
            await _storageProvider.AddFacesAsync(name, DateOnly.FromDateTime(DateTime.UtcNow.Add(defaultTtlFace)), null,
                faces.First());
        }

        internal async Task<ApiResults<FaceMatch>> Process(HttpContext context, bool? async = default)
        {
            // TODO: for async calls, get it from requests and write to file and add to db
            // add to queue?

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
            var faces = (await _faceDetector.FaceDetectAsync(profile.PathToImage)).ToList();
            if (!faces.Any())
            {
                // TODO: add reason
                return new ApiResults<FaceMatch>
                {
                    Error = new ApiError { StatusCode = 400, Message = "No face found!" }
                };
            }

            var faceIds = await _storageProvider.GetKnownFacesAsync();

            var verifyResults = await _faceDetector.FaceVerifyAsync(faces.First().Id, faceIds);

            var res = new FaceMatch
            {
                Identified = verifyResults.Any(x => x.IsIdentical),
                Name = verifyResults.Where(x => x.IsIdentical)
                    .Select(x => x.Person)
                    .FirstOrDefault()
            };

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