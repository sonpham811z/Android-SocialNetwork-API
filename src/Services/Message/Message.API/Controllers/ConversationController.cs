using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Message.Application.DTOs;
using Message.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Message.API.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationController : ControllerBase
{
    private readonly IConversationService _service;
    private readonly ILogger<ConversationController> _logger;

    public ConversationController(
        IConversationService             service,
        ILogger<ConversationController>  logger)
    {
        _service = service;
        _logger  = logger;
    }

    private Guid CurrentUserId =>
        Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub")!);

    /// <summary>Get all conversations for the current user, sorted by most recently updated.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ConversationDto>>), 200)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ConversationDto>>>> GetConversations()
    {
        var result = await _service.GetUserConversationsAsync(CurrentUserId);
        return Ok(ApiResponse<IEnumerable<ConversationDto>>.Ok(result));
    }

    /// <summary>Get a specific conversation by ID (current user must be a member).</summary>
    [HttpGet("{conversationId}")]
    [ProducesResponseType(typeof(ApiResponse<ConversationDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> GetConversation(string conversationId)
    {
        var result = await _service.GetConversationAsync(conversationId, CurrentUserId);
        if (result is null)
            return NotFound(ApiResponse<ConversationDto>.Fail("Conversation not found."));

        return Ok(ApiResponse<ConversationDto>.Ok(result));
    }

    /// <summary>
    /// Create or get an existing 1-1 conversation with another user.
    /// Requires the target user to be a friend (verified via Friend Service gRPC).
    /// </summary>
    [HttpPost("one-to-one")]
    [ProducesResponseType(typeof(ApiResponse<ConversationDto>), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> CreateOneToOne(
        [FromBody] CreateOneToOneConversationDto dto)
    {

        var result = await _service.CreateOneToOneAsync(CurrentUserId, dto.TargetUserId);
        return CreatedAtAction(
            nameof(GetConversation),
            new { conversationId = result.Id },
            ApiResponse<ConversationDto>.Ok(result, "Conversation created."));
    }

    /// <summary>
    /// Create a new group conversation.
    /// At least 2 other members are required.
    /// </summary>
    [HttpPost("group")]
    [ProducesResponseType(typeof(ApiResponse<ConversationDto>), 201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> CreateGroup(
        [FromBody] CreateGroupConversationDto dto)
    {
        var result = await _service.CreateGroupAsync(CurrentUserId, dto);
        return CreatedAtAction(
            nameof(GetConversation),
            new { conversationId = result.Id },
            ApiResponse<ConversationDto>.Ok(result, "Group conversation created."));
    }
}
