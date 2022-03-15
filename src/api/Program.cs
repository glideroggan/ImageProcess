using api;
using azure_face;
using contracts;
using storage_sqllite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<ImageProcessor>();
builder.Services.AddSingleton<IFaceDetector, AzureFaceServices>();
builder.Services.AddSingleton<IStorageProvider, StorageSqlLite>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<HostedServiceFaceDetect>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
var defaultOptions = new DefaultFilesOptions();
defaultOptions.DefaultFileNames.Clear();
defaultOptions.DefaultFileNames.Add("index.html");
app.UseDefaultFiles(defaultOptions);
app.UseStaticFiles();

//app.UseAuthorization();


var imageProcess = app.Services.GetRequiredService<ImageProcessor>();
// TODO: we want parameter to this if the call is async or not
app.MapPost("/api/image", async (ctx) =>
    await imageProcess.Process(ctx));
// TODO: we want to be able to send in an faceId here, so that we can tell that it belongs to the same face
app.MapPost("/api/upload/{name}", async (string name, HttpContext ctx) => await imageProcess.AddNewFace(ctx, name));

app.Run();