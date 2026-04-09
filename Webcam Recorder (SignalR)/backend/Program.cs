using WebcamRecorderApi.Services;
using WebcamRecorderApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSingleton<VideoStorageService>();
builder.Services.AddSingleton<StreamSessionManager>();
builder.Services.AddSignalR(options =>
{
    // Increase max message size for video chunk uploads (default is 32KB)
    // Binary data serialized as JSON becomes ~33% larger, so we need buffer
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
    // Keep-alive interval to detect dropped connections
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60); // Increased from 30s to 60s
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
}).AddMessagePackProtocol();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAny", policy =>
    {
        policy.WithOrigins("http://localhost:1989")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure middleware
app.UseCors("AllowAny");
app.UseRouting();
app.MapControllers();
app.MapHub<VideoStreamHub>("/hub/video-stream");

// Create videos directory if it doesn't exist
var storagePath = Path.Combine(Directory.GetCurrentDirectory(), app.Configuration["VideoStorage:StoragePath"]!);
Directory.CreateDirectory(storagePath);

app.Run();
