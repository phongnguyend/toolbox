namespace WebcamRecorderApi.Models;

public class AcsSession
{
    public string RoomId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsLive { get; set; }
    public string? VideoSessionId { get; set; }
}
