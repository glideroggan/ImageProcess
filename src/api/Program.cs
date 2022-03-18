using api;
using azure_face;
using contracts;
using DlibFaceDetector;
using storage_sqllite;

/* TODO:
 *  - Change so that when adding a person, the snapshot is overtaking the camera feed for some seconds
 *  - Break out the local service MyFaceDetector to its own project
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
// TODO: register both detectors, and then use features, so that we can choose a detector that have the feature
// we want
// local service
builder.Services.AddSingleton<IFaceDetector, MyFaceDetector>();
// Azure
// builder.Services.AddSingleton<IFaceDetector>(provider =>
// {
//     var configuration = provider.GetRequiredService<IConfiguration>();
//     var opt = new AzureFaceServicesOptions();
//     configuration.GetSection(AzureFaceServicesOptions.AzureFaceServices).Bind(opt);
//     return new AzureFaceServices(opt.ApiKey, opt.Endpoint);
// });
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
// TODO: we want parameter to this if the call is async or not
app.MapPost("/api/verify", async (HttpContext ctx, bool? async) =>
    await imageProcess.VerifyFace(ctx, async));
// TODO: we want to be able to send in an faceId here, so that we can tell that it belongs to the same face
app.MapPost("/api/upload/{name}", async (string name, HttpContext ctx) => await imageProcess.AddNewFace(ctx, name));
// TODO: try to create a nice REST structure for faces
app.MapGet("/api/faces/{faceId}/attributes", async (Guid faceId, HttpContext ctx) => await imageProcess.GetAttributes(ctx, faceId));
app.MapGet("/api/faces", async (ctx) => await imageProcess.GetFaces(ctx));

app.Run();