using System.Buffers;

namespace api
{
    public static class ImageProcessor
    {
        internal static async Task Process(HttpContext context)
        {
            // get data from request
            var data = new Memory<byte>(new byte[(int)context.Request.ContentLength]);
            var reader = context.Request.BodyReader;
            while (true)
            {
                var readResult = await reader.ReadAsync();
                var buffer = readResult.Buffer;
                
                // TODO: copy over buffer if we didn't get everything in one go
                buffer.CopyTo(data.Span);

                if (readResult.IsCompleted) break;
                throw new NotImplementedException("If we got here, we need to do the above TODO");
            }
            
            // save file as image
            using var fileStream = File.Create("processFolder/image.jpg");
            await fileStream.WriteAsync(data);
            await fileStream.FlushAsync();
        }
    }
}
