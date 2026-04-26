using Microsoft.AspNetCore.Mvc;
using WebcamRecorderApi.Services;

namespace WebcamRecorderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoController : ControllerBase
{
    private readonly VideoStorageService _storageService;
    private readonly ILogger<VideoController> _logger;

    public VideoController(VideoStorageService storageService, ILogger<VideoController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    [HttpPost("chunk")]
    public async Task<IActionResult> UploadChunk([FromQuery] string sessionId, [FromQuery] int chunkIndex)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
            var chunkData = memoryStream.ToArray();

            var success = await _storageService.SaveChunk(sessionId, chunkIndex, chunkData);
            if (success)
            {
                return Ok(new { message = "Chunk saved successfully" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to save chunk" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error uploading chunk: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("finalize")]
    public async Task<IActionResult> FinalizeVideo([FromQuery] string sessionId, [FromQuery] string filename)
    {
        try
        {
            var success = await _storageService.FinalizeVideo(sessionId, filename);
            if (success)
            {
                return Ok(new { message = "Video finalized successfully", videoName = $"{filename}.webm" });
            }
            else
            {
                return StatusCode(500, new { error = "Failed to finalize video" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error finalizing video: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("list")]
    public IActionResult GetVideos()
    {
        try
        {
            var videos = _storageService.GetVideos()
                .Select(v => new { name = v.Name, createdAt = v.CreatedAt });
            return Ok(new { videos });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error listing videos: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{filename}")]
    public IActionResult GetVideo(string filename)
    {
        try
        {
            var videoStream = _storageService.GetVideo(filename);
            if (videoStream == null)
            {
                return NotFound(new { error = "Video not found" });
            }

            return File(videoStream, "video/webm", filename, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving video: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{filename}")]
    public IActionResult DeleteVideo(string filename)
    {
        try
        {
            var videoPath = Path.Combine(_storageService.GetStoragePath(), filename);
            if (System.IO.File.Exists(videoPath))
            {
                System.IO.File.Delete(videoPath);
                return Ok(new { message = "Video deleted successfully" });
            }
            return NotFound(new { error = "Video not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting video: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
