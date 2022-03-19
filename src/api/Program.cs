using api;
using azure_face;
using contracts;
using DlibFaceDetector;
using storage_sqllite;

/* TODO:
 *  - Change so that when adding a person, the snapshot is overtaking the camera feed for some seconds
 * What happens if?
 *  - verify
 *      - and nothing in db?
 *      - and no hit?
 *  - upload
 *      - and no face found?
 *      - and no image in data?
 */

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<ImageProcessor>();
builder.Services.AddSingleton<IFaceDetector, FaceDetector>();
// local service
builder.Services.AddSingleton<IFacePlugin, DlibFace>();
// Azure
builder.Services.AddSingleton<IFacePlugin>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var opt = new AzureFaceServicesOptions();
    configuration.GetSection(AzureFaceServicesOptions.AzureFaceServices).Bind(opt);
    return new AzureFaceServices(opt.ApiKey, opt.Endpoint);
});
builder.Services.AddSingleton<IStorageProvider, StorageSqlLite>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// FEATURE: this will be needed once we enable async feature on the verified endpoint
// builder.Services.AddHostedService<HostedServiceFaceDetect>();

var app = builder.Build();

app.UseHttpsRedirection();

var defaultOptions = new DefaultFilesOptions();
defaultOptions.DefaultFileNames.Clear();
defaultOptions.DefaultFileNames.Add("index.html");
app.UseDefaultFiles(defaultOptions);
app.UseStaticFiles();

var imageProcess = app.Services.GetRequiredService<ImageProcessor>();

/*
 * GET "/api/faces/{faceId}/attributes" -> returns a list of attributes from storage about this faceId      
 * POST "/api/faces/attributes" -> returns a list of attributes of the streamed image
 * 
 */
app.MapGet("/api/faces", async (ctx) => await imageProcess.GetFaces(ctx));
app.MapPost("/api/faces/{name}", async (string name, HttpContext ctx) => await imageProcess.AddNewFace(ctx, name));
app.MapGet("/api/faces/{faceId}", async (HttpContext ctx, Guid faceId) => await imageProcess.GetFaces(ctx, faceId));
app.MapPost("/api/faces/verify", async (HttpContext ctx, bool? async) => await imageProcess.VerifyFace(ctx));

// TODO: needs to be implemented
app.MapGet("/api/faces/{faceId}/attributes", async (Guid faceId, HttpContext ctx) =>
    await imageProcess.GetAttributes(ctx, faceId));
app.MapPost("/api/faces/attributes", async ctx => await imageProcess.GetAttributes(ctx));

app.Run();