using Azure.Communication;
using Azure.Communication.Identity;
using Azure.Communication.Rooms;
using Microsoft.AspNetCore.Mvc;
using WebcamRecorderApi.Services;

namespace WebcamRecorderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AcsController : ControllerBase
{
    private readonly CommunicationIdentityClient _identityClient;
    private readonly RoomsClient _roomsClient;
    private readonly AcsSessionManager _sessionManager;
    private readonly VideoStorageService _storageService;
    private readonly ILogger<AcsController> _logger;

    public AcsController(
        CommunicationIdentityClient identityClient,
        RoomsClient roomsClient,
        AcsSessionManager sessionManager,
        VideoStorageService storageService,
        ILogger<AcsController> logger)
    {
        _identityClient = identityClient;
        _roomsClient = roomsClient;
        _sessionManager = sessionManager;
        _storageService = storageService;
        _logger = logger;
    }

    // POST api/acs/token — create a new ACS identity and return a VoIP token
    [HttpPost("token")]
    public async Task<IActionResult> GetToken()
    {
        try
        {
            var response = await _identityClient.CreateUserAndTokenAsync(
                [CommunicationTokenScope.VoIP]);

            var user = response.Value.User;
            var token = response.Value.AccessToken;

            _logger.LogInformation("Created ACS user {UserId}", user.Id);

            return Ok(new
            {
                userId = user.Id,
                token = token.Token,
                expiresOn = token.ExpiresOn
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ACS token");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // POST api/acs/rooms — create a new ACS room and video session, return roomId
    [HttpPost("rooms")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        try
        {
            var validFrom = DateTimeOffset.UtcNow;
            var validUntil = validFrom.AddHours(24);

            var createRoomOptions = new CreateRoomOptions
            {
                ValidFrom = validFrom,
                ValidUntil = validUntil
            };

            var roomResponse = await _roomsClient.CreateRoomAsync(createRoomOptions);
            var roomId = roomResponse.Value.Id;

            // Add the recorder user as a Presenter in the room
            if (!string.IsNullOrEmpty(request.UserId))
            {
                var participants = new List<RoomParticipant>
                {
                    new RoomParticipant(new CommunicationUserIdentifier(request.UserId))
                    {
                        Role = ParticipantRole.Presenter
                    }
                };
                await _roomsClient.AddOrUpdateParticipantsAsync(roomId, participants);
                _logger.LogInformation("Added recorder user {UserId} as Presenter in room {RoomId}", request.UserId, roomId);
            }

            // Create a video storage session for this room
            var videoSessionId = Guid.NewGuid().ToString("N");
            _storageService.CreateVideoSession(videoSessionId);

            // Register the session in the manager
            var session = _sessionManager.CreateSession(roomId, request.Title ?? "Recording");
            _sessionManager.SetVideoSessionId(roomId, videoSessionId);

            _logger.LogInformation("Created ACS room {RoomId} with video session {VideoSessionId}", roomId, videoSessionId);

            return Ok(new
            {
                roomId,
                videoSessionId,
                title = session.Title,
                createdAt = session.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ACS room");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // GET api/acs/rooms — list all active rooms
    [HttpGet("rooms")]
    public IActionResult GetRooms()
    {
        var sessions = _sessionManager.GetAllSessions()
            .Where(s => s.IsLive)
            .Select(s => new
            {
                roomId = s.RoomId,
                title = s.Title,
                createdAt = s.CreatedAt,
                isLive = s.IsLive,
                videoSessionId = s.VideoSessionId
            });

        return Ok(sessions);
    }

    // POST api/acs/rooms/{roomId}/token — generate a join token for a viewer
    [HttpPost("rooms/{roomId}/token")]
    public async Task<IActionResult> GetJoinToken(string roomId)
    {
        try
        {
            var session = _sessionManager.GetSession(roomId);
            if (session == null)
            {
                return NotFound(new { error = "Room not found" });
            }

            var response = await _identityClient.CreateUserAndTokenAsync(
                [CommunicationTokenScope.VoIP]);

            var user = response.Value.User;
            var token = response.Value.AccessToken;

            // Add the user as an attendee in the room
            var participants = new List<RoomParticipant>
            {
                new RoomParticipant(new CommunicationUserIdentifier(user.Id))
                {
                    Role = ParticipantRole.Attendee
                }
            };
            await _roomsClient.AddOrUpdateParticipantsAsync(roomId, participants);

            _logger.LogInformation("Created join token for room {RoomId}, user {UserId}", roomId, user.Id);

            return Ok(new
            {
                userId = user.Id,
                token = token.Token,
                expiresOn = token.ExpiresOn,
                roomId,
                title = session.Title
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating join token for room {RoomId}", roomId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // DELETE api/acs/rooms/{roomId} — end a room session
    [HttpDelete("rooms/{roomId}")]
    public async Task<IActionResult> EndRoom(string roomId)
    {
        try
        {
            _sessionManager.EndSession(roomId);
            _logger.LogInformation("Ended room session {RoomId}", roomId);
            return Ok(new { message = "Room session ended" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending room {RoomId}", roomId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public record CreateRoomRequest(string? Title, string? UserId);
