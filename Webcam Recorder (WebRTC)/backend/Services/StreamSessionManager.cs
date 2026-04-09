using WebcamRecorderApi.Models;

namespace WebcamRecorderApi.Services;

public class StreamSessionManager
{
    private readonly Dictionary<string, StreamSession> _sessions = new();
    private readonly ILogger<StreamSessionManager> _logger;

    public StreamSessionManager(ILogger<StreamSessionManager> logger)
    {
        _logger = logger;
    }

    public StreamSession CreateSession(string title)
    {
        var session = new StreamSession { Title = title };
        _sessions[session.SessionId] = session;
        _logger.LogInformation($"Session created: {session.SessionId}");
        return session;
    }

    public StreamSession? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    public List<StreamSession> GetAllSessions()
    {
        return _sessions.Values.ToList();
    }

    public void RemoveSession(string sessionId)
    {
        if (_sessions.Remove(sessionId))
        {
            _logger.LogInformation($"Session removed: {sessionId}");
        }
    }

    public async Task RemoveViewerAsync(string connectionId)
    {
        foreach (var session in _sessions.Values)
        {
            session.ViewerConnectionIds.Remove(connectionId);
        }
    }
}
