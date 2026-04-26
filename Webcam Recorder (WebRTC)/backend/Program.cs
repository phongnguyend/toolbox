using Azure.Communication.Identity;
using Azure.Communication.Rooms;
using WebcamRecorderApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSingleton<VideoStorageService>();
builder.Services.AddSingleton<AcsSessionManager>();

// Register ACS clients
var acsConnectionString = builder.Configuration["AzureCommunicationServices:ConnectionString"]!;
builder.Services.AddSingleton(new CommunicationIdentityClient(acsConnectionString));
builder.Services.AddSingleton(new RoomsClient(acsConnectionString));

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAny", policy =>
    {
        policy.WithOrigins("http://localhost:1989")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware
app.UseCors("AllowAny");
app.UseRouting();
app.MapControllers();

// Create videos directory if it doesn't exist
var storagePath = Path.Combine(Directory.GetCurrentDirectory(), app.Configuration["VideoStorage:StoragePath"]!);
Directory.CreateDirectory(storagePath);

app.Run();
