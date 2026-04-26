using System.Threading.Channels;

namespace WebcamRecorderApi.Services;

public class VideoStorageService : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<VideoStorageService> _logger;
    private readonly Dictionary<string, Channel<(int Index, byte[] Data)>> _sessionChannels = new();
    private readonly Dictionary<string, Task> _sessionWriteTasks = new();
    private readonly Dictionary<string, CancellationTokenSource> _sessionCancellationTokens = new();
    private readonly object _sessionLock = new();
    private const int FlushIntervalMs = 500;

    public VideoStorageService(IConfiguration configuration, ILogger<VideoStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GetStoragePath()
    {
        var basePath = _configuration["VideoStorage:StoragePath"]!;
        return Path.Combine(Directory.GetCurrentDirectory(), basePath);
    }

    public void CreateVideoSession(string sessionId)
    {
        var sessionPath = Path.Combine(GetStoragePath(), sessionId);
        Directory.CreateDirectory(sessionPath);

        lock (_sessionLock)
        {
            // Create a channel for this session with a bounded capacity
            var channel = Channel.CreateBounded<(int, byte[])>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

            var cts = new CancellationTokenSource();
            _sessionChannels[sessionId] = channel;
            _sessionCancellationTokens[sessionId] = cts;

            // Start the background write task for this session
            _sessionWriteTasks[sessionId] = WriteChunksToFileAsync(sessionId, channel, cts.Token);
        }

        _logger.LogInformation($"Created video session: {sessionId}");
    }

    public async Task<bool> SaveChunk(string sessionId, int chunkIndex, byte[] data)
    {
        lock (_sessionLock)
        {
            if (!_sessionChannels.TryGetValue(sessionId, out var channel))
            {
                _logger.LogWarning($"Channel not found for session {sessionId}");
                return false;
            }

            if (!channel.Writer.TryWrite((chunkIndex, data)))
            {
                _logger.LogWarning($"Failed to queue chunk {chunkIndex} for session {sessionId}");
                return false;
            }
        }

        _logger.LogInformation($"Queued chunk {chunkIndex} for session {sessionId} (size: {data.Length} bytes)");
        return true;
    }

    private async Task WriteChunksToFileAsync(string sessionId, Channel<(int Index, byte[] Data)> channel, CancellationToken cancellationToken)
    {
        var videoPath = Path.Combine(GetStoragePath(), sessionId, "recording.webm");

        try
        {
            // FileMode.Create creates the file initially, then the FileStream remains open
            // All subsequent WriteAsync calls append to this stream without truncating
            using (var fileStream = new FileStream(videoPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 65536, useAsync: true))
            {
                var flushTask = FlushPeriodicallyAsync(fileStream, cancellationToken);

                try
                {
                    await foreach (var (index, data) in channel.Reader.ReadAllAsync(cancellationToken))
                    {
                        try
                        {
                            // Each WriteAsync appends data to the file at the current position
                            await fileStream.WriteAsync(data, 0, data.Length, cancellationToken);
                            _logger.LogDebug($"Appended chunk {index} to file for session {sessionId} (size: {data.Length} bytes, position: {fileStream.Position})");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error writing chunk {index} to file: {ex.Message}", ex);
                        }
                    }
                }
                finally
                {
                    // Ensure final flush and cleanup
                    await fileStream.FlushAsync(cancellationToken);
                    await flushTask;
                }
            }

            _logger.LogInformation($"Completed writing all chunks to file for session {sessionId}: {videoPath}");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation($"Write task cancelled for session {sessionId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in WriteChunksToFileAsync for session {sessionId}: {ex.Message}", ex);
        }
    }

    private async Task FlushPeriodicallyAsync(FileStream fileStream, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(FlushIntervalMs, cancellationToken);
                    await fileStream.FlushAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            if (ex is not ObjectDisposedException)
            {
                // Log but continue
            }
        }
    }

    public async Task<bool> FinalizeVideo(string sessionId, string filename)
    {
        lock (_sessionLock)
        {
            if (_sessionChannels.TryGetValue(sessionId, out var channel))
            {
                // Signal that no more chunks will be written
                channel.Writer.TryComplete();
                _sessionChannels.Remove(sessionId);
            }

            // Cancel the flush task
            if (_sessionCancellationTokens.TryGetValue(sessionId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _sessionCancellationTokens.Remove(sessionId);
            }
        }

        // Wait for the write task to complete
        if (_sessionWriteTasks.TryGetValue(sessionId, out var writeTask))
        {
            try
            {
                await writeTask;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error waiting for write task to complete: {ex.Message}", ex);
                return false;
            }
            finally
            {
                _sessionWriteTasks.Remove(sessionId);
            }
        }

        // Rename the temp file to the final filename
        var sessionPath = Path.Combine(GetStoragePath(), sessionId);
        var tempVideoPath = Path.Combine(sessionPath, "recording.webm");
        var finalVideoPath = Path.Combine(GetStoragePath(), $"{filename}.webm");

        if (!File.Exists(tempVideoPath))
        {
            _logger.LogWarning($"Temp video file not found: {tempVideoPath}");
            return false;
        }

        try
        {
            File.Move(tempVideoPath, finalVideoPath, overwrite: true);
            Directory.Delete(sessionPath);
            _logger.LogInformation($"Finalized video for session {sessionId}: {finalVideoPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error finalizing video: {ex.Message}", ex);
            return false;
        }
    }

    public List<(string Name, DateTimeOffset CreatedAt)> GetVideos()
    {
        var storagePath = GetStoragePath();
        return Directory.GetFiles(storagePath, "*.webm")
            .Select(f => (Name: Path.GetFileName(f)!, CreatedAt: new DateTimeOffset(File.GetCreationTimeUtc(f), TimeSpan.Zero)))
            .OrderByDescending(v => v.CreatedAt)
            .ToList();
    }

    public FileStream? GetVideo(string filename)
    {
        var videoPath = Path.Combine(GetStoragePath(), filename);
        if (File.Exists(videoPath))
        {
            // Return a FileStream for streaming large files
            // FileShare.Read allows other processes to read the file while we're streaming
            return new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync: true);
        }
        return null;
    }

    public string GetVideoPath(string filename)
    {
        return Path.Combine(GetStoragePath(), filename);
    }

    public async ValueTask DisposeAsync()
    {
        lock (_sessionLock)
        {
            foreach (var channel in _sessionChannels.Values)
            {
                channel.Writer.TryComplete();
            }
            _sessionChannels.Clear();

            foreach (var cts in _sessionCancellationTokens.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _sessionCancellationTokens.Clear();
        }

        try
        {
            await Task.WhenAll(_sessionWriteTasks.Values);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error disposing: {ex.Message}", ex);
        }

        _sessionWriteTasks.Clear();
    }
}
