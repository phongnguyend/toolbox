using System.Collections.Concurrent;
using WebcamRecorderApi.Models;

namespace WebcamRecorderApi.Services;

public class AcsSessionManager
{
    private readonly ConcurrentDictionary<string, AcsSession> _sessions = new();

    public AcsSession CreateSession(string roomId, string title)
    {
        var session = new AcsSession
        {
            RoomId = roomId,
            Title = title,
            CreatedAt = DateTimeOffset.UtcNow,
            IsLive = true
        };
        _sessions[roomId] = session;
        return session;
    }

    public AcsSession? GetSession(string roomId) =>
        _sessions.TryGetValue(roomId, out var s) ? s : null;

    public IReadOnlyList<AcsSession> GetAllSessions() =>
        _sessions.Values.ToList();

    public bool EndSession(string roomId)
    {
        if (_sessions.TryGetValue(roomId, out var session))
        {
            session.IsLive = false;
            return true;
        }
        return false;
    }

    public void SetVideoSessionId(string roomId, string videoSessionId)
    {
        if (_sessions.TryGetValue(roomId, out var session))
        {
            session.VideoSessionId = videoSessionId;
        }
    }
}
