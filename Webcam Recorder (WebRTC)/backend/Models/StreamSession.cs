namespace WebcamRecorderApi.Models;

public class StreamSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsLive { get; set; } = true;
    public long TotalBytesReceived { get; set; }
    public string? SavedFilepath { get; set; }
    public HashSet<string> ViewerConnectionIds { get; set; } = new HashSet<string>();
    public byte[]? BootstrapData { get; set; }
}
