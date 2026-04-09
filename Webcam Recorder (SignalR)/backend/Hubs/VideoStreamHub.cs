using Microsoft.AspNetCore.SignalR;
using WebcamRecorderApi.Services;

namespace WebcamRecorderApi.Hubs;

public class VideoStreamHub : Hub
{
    private readonly StreamSessionManager _sessionManager;
    private readonly VideoStorageService _storageService;
    private readonly ILogger<VideoStreamHub> _logger;

    public VideoStreamHub(StreamSessionManager sessionManager, VideoStorageService storageService, ILogger<VideoStreamHub> logger)
    {
        _sessionManager = sessionManager;
        _storageService = storageService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"[HUB] ✅ Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError($"Client disconnected with error: {Context.ConnectionId}, Exception: {exception.Message}", exception);
        }
        else
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        }
        
        await _sessionManager.RemoveViewerAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // Recorder: Start a new stream session
    public async Task<string> StartStream(string title)
    {
        var session = _sessionManager.CreateSession(title);
        _storageService.CreateVideoSession(session.SessionId, session);
        _logger.LogInformation($"Stream started: {session.SessionId} - {title}");
        
        // Notify all clients of the new stream
        await Clients.All.SendAsync("StreamStarted", session.SessionId, title);
        
        return session.SessionId;
    }

    // Recorder: Upload a chunk of video data
    public async Task UploadChunk(string sessionId, int chunkIndex, byte[] bytes)
    {
        try
        {
            _logger.LogInformation($"[HUB] UploadChunk called: sessionId={sessionId}, chunkIndex={chunkIndex}, bytesSize={bytes?.Length ?? 0}");
            
            // Validate inputs
            if (string.IsNullOrEmpty(sessionId))
            {
                _logger.LogWarning("[HUB] Empty session ID received");
                await Clients.Caller.SendAsync("Error", "Session ID cannot be empty");
                return;
            }

            if (bytes == null || bytes.Length == 0)
            {
                _logger.LogWarning($"[HUB] Empty chunk received for session {sessionId}");
                await Clients.Caller.SendAsync("Error", "Chunk data cannot be empty");
                return;
            }

            // Get the session
            var session = _sessionManager.GetSession(sessionId);
            if (session == null)
            {
                _logger.LogWarning($"[HUB] Session not found: {sessionId}");
                await Clients.Caller.SendAsync("Error", $"Session {sessionId} not found");
                return;
            }

            if (!session.IsLive)
            {
                _logger.LogWarning($"[HUB] Session is not live: {sessionId}");
                await Clients.Caller.SendAsync("Error", $"Session {sessionId} is not live");
                return;
            }



            // Save the chunk to disk immediately to prevent unbounded memory growth.
            var saved = await _storageService.SaveChunk(session.SessionId, chunkIndex, bytes);
            if (!saved)
            {
                _logger.LogWarning($"[HUB] Failed to save chunk {chunkIndex} for session {sessionId}");
                await Clients.Caller.SendAsync("Error", "Failed to save video chunk");
                return;
            }

            session.TotalBytesReceived += bytes.Length;

            _logger.LogInformation($"[HUB] Chunk {chunkIndex} saved for session {sessionId} (size: {bytes.Length} bytes, total: {session.TotalBytesReceived} bytes)");

            // Notify viewers with the chunk data for live playback
            try
            {
                await Clients.Group(sessionId).SendAsync("ReceiveChunk", bytes);
                _logger.LogDebug($"[HUB] Chunk {chunkIndex} sent to viewers for session {sessionId} (size: {bytes.Length} bytes)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[HUB] Failed to notify viewers: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            // Catch-all for any unexpected errors
            _logger.LogError($"[HUB] ⚠️ UNEXPECTED ERROR in UploadChunk: {ex.GetType().Name}: {ex.Message}", ex);
            try
            {
                await Clients.Caller.SendAsync("Error", $"Unexpected server error: {ex.GetType().Name}");
            }
            catch (Exception innerEx)
            {
                _logger.LogError($"[HUB] Failed to send error message to client: {innerEx.Message}");
            }
        }
    }

    // Recorder: Finalize the stream and save video
    public async Task<string> FinalizeStream(string sessionId, string filename)
    {
        var session = _sessionManager.GetSession(sessionId);
        if (session == null)
        {
            _logger.LogWarning($"Session not found: {sessionId}");
            return "Error: Session not found";
        }

        session.IsLive = false;

        // Finalize the video file from the saved chunks on disk.
        var success = await _storageService.FinalizeVideo(session.SessionId, filename);
        if (!success)
        {
            _logger.LogWarning($"Failed to finalize video for session {sessionId}");
            return "Error: Failed to finalize video";
        }

        session.SavedFilepath = _storageService.GetVideoPath($"{filename}.webm");
        _logger.LogInformation($"Video saved: {session.SavedFilepath}");

        // Notify viewers that stream ended
        await Clients.Group(sessionId).SendAsync("StreamEnded", filename);

        return filename;
    }

    // Viewer: Join a stream session
    public async Task JoinStream(string sessionId)
    {
        var session = _sessionManager.GetSession(sessionId);
        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", "Stream not found");
            return;
        }

        // Add viewer to session
        session.ViewerConnectionIds.Add(Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

        _logger.LogInformation($"Viewer {Context.ConnectionId} joined stream {sessionId}");

        // Send stream info to viewer
        await Clients.Caller.SendAsync("StreamInfo", new
        {
            sessionId,
            title = session.Title,
            isLive = session.IsLive,
            startedAt = session.CreatedAt,
            totalBytesReceived = session.TotalBytesReceived
        });

        // Note: Bootstrap chunks are now fetched via GET /api/video/bootstrap/{sessionId}
        // This keeps SignalR focused on real-time streaming only
        _logger.LogInformation($"[HUB] Viewer {Context.ConnectionId} can fetch bootstrap via API");
    }

    // Get list of all available streams
    public List<object> GetAvailableStreams()
    {
        var sessions = _sessionManager.GetAllSessions();
        return sessions.Select(s => new
        {
            sessionId = s.SessionId,
            title = s.Title,
            isLive = s.IsLive,
            createdAt = s.CreatedAt,
            savedFile = s.SavedFilepath
        }).Cast<object>().ToList();
    }

    // Get list of archived (recorded) streams
    public List<object> GetArchivedStreams()
    {
        var videos = _storageService.GetVideos();
        return videos.Select(filename => new
        {
            filename,
            createdAt = System.IO.File.GetCreationTime(_storageService.GetVideoPath(filename))
        }).Cast<object>().ToList();
    }
}
