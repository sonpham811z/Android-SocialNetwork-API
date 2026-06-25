using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Message.Application.DTOs;
using Message.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Message.API.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IMessageService _service;
    private readonly IMediaService   _media;

    public MessageController(IMessageService service, IMediaService media)
    {
        _service = service;
        _media   = media;
    }

    private Guid CurrentUserId =>
        Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub")!);

    /// <summary>
    /// Send a message via REST API.
    /// Equivalent to the SignalR hub's SendMessage — both persist to MongoDB and broadcast to the group.
    /// Use this for non-realtime clients or server-to-server messaging.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MessageDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage([FromBody] SendMessageDto dto)
    {
        var result = await _service.SendMessageAsync(CurrentUserId, dto);
        return Ok(ApiResponse<MessageDto>.Ok(result, "Message sent."));
    }

    /// <summary>
    /// Upload an image attachment for a chat message.
    /// Returns the public Cloudinary URL — the client then sends a message
    /// (via SignalR or POST /api/messages) with Type=Image and Content set to this URL.
    /// </summary>
    [HttpPost("upload-image")]
    [ProducesResponseType(typeof(ApiResponse<MediaUploadResponseDto>), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<ApiResponse<MediaUploadResponseDto>>> UploadImage([FromForm] IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<MediaUploadResponseDto>.Fail("Image file is required."));

        await using var stream = file.OpenReadStream();
        var uploaded = await _media.UploadImageAsync(stream, file.FileName, file.Length);

        var dto = new MediaUploadResponseDto { Url = uploaded.Url, PublicId = uploaded.PublicId };
        return Ok(ApiResponse<MediaUploadResponseDto>.Ok(dto, "Image uploaded."));
    }

    /// <summary>
    /// Get message history for a conversation using keyset (cursor-based) pagination.
    ///
    /// First page:  GET /api/messages/{conversationId}?pageSize=30
    /// Next pages:  GET /api/messages/{conversationId}?beforeMessageId={NextCursor}&amp;pageSize=30
    ///
    /// Results are sorted newest-first. Use NextCursor from the response to load older messages.
    /// </summary>
    [HttpGet("{conversationId}")]
    [ProducesResponseType(typeof(ApiResponse<MessagePageDto>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ApiResponse<MessagePageDto>>> GetMessages(
        string  conversationId,
        [FromQuery] string? beforeMessageId = null,
        [FromQuery] int     pageSize        = 30)
    {
        var result = await _service.GetMessagesAsync(
            conversationId, CurrentUserId, beforeMessageId, pageSize);

        return Ok(ApiResponse<MessagePageDto>.Ok(result));
    }

    /// <summary>
    /// Mark all unread messages in the conversation as read for the current user.
    /// Broadcasts a ReadReceipt event to all conversation members via SignalR.
    /// </summary>
    [HttpPatch("{conversationId}/read")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ApiResponse<object>>> MarkAsRead(string conversationId)
    {
        await _service.MarkAsReadAsync(conversationId, CurrentUserId);
        return Ok(ApiResponse<object>.Ok(null!, "Marked as read."));
    }
}
