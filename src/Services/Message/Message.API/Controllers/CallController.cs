using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Message.Application.DTOs;
using Message.Application.Interfaces;
using Message.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Message.API.Controllers;

[ApiController]
[Route("api/call")]
[Authorize]
public class CallController : ControllerBase
{
    private readonly IAgoraTokenService _agoraTokenService;
    private readonly AgoraSettings      _settings;

    public CallController(IAgoraTokenService agoraTokenService, IOptions<AgoraSettings> options)
    {
        _agoraTokenService = agoraTokenService;
        _settings          = options.Value;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User.FindFirstValue("sub")!;

    /// <summary>
    /// Generate an Agora RTC token for a voice or video call.
    /// channelName should be the conversationId.
    /// Both callers must request a token for the same channelName to join the same call.
    /// </summary>
    [HttpGet("token")]
    [ProducesResponseType(typeof(ApiResponse<AgoraTokenResponse>), 200)]
    [ProducesResponseType(400)]
    public ActionResult<ApiResponse<AgoraTokenResponse>> GetToken(
        [FromQuery] string channelName,
        [FromQuery] bool   isVideo = false)
    {
        if (string.IsNullOrWhiteSpace(channelName))
            return BadRequest(ApiResponse<AgoraTokenResponse>.Fail("channelName is required."));

        var token    = _agoraTokenService.GenerateRtcToken(channelName, CurrentUserId, isPublisher: true);
        var expireAt = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _settings.TokenExpireSeconds;

        return Ok(ApiResponse<AgoraTokenResponse>.Ok(
            new AgoraTokenResponse(token, _settings.AppId, channelName, expireAt),
            "Token generated."));
    }
}
