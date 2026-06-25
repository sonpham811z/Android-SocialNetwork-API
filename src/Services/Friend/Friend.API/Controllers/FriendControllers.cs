using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Friend.Application.DTOs;
using Friend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Friend.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/friends")]
    public class FriendController : ControllerBase
    {
        private readonly IFriendService _friendService;
        private readonly IFriendRequestService _requestService;
        private readonly ILogger<FriendController> _logger;

        public FriendController(IFriendService friendService, IFriendRequestService requestService, ILogger<FriendController> logger)
        {
            _friendService  = friendService;
            _requestService = requestService;
            _logger = logger;
        }

        private Guid CurrentUserId
        {
            get
            {
                var userIdClaim =
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                    User.FindFirstValue("sub");

                return Guid.Parse(userIdClaim!);
            }
        }

        // GET api/friends?page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetMyFriends([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _friendService.GetFriendsAsync(CurrentUserId, page, pageSize);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // GET api/friends/{userId}
        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetFriends(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _friendService.GetFriendsAsync(userId, page, pageSize);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // GET api/friends/ids  — used internally by Post service for feed
        [HttpGet("ids")]
        public async Task<IActionResult> GetFriendIds()
        {
            var result = await _friendService.GetFriendIdsAsync(CurrentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // DELETE api/friends/{targetUserId}
        [HttpDelete("{targetUserId:guid}")]
        public async Task<IActionResult> Unfriend(Guid targetUserId)
        {
            var result = await _friendService.UnfriendAsync(CurrentUserId, targetUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // GET api/friends/suggestions?limit=10  — "People you may know" (friends-of-friends)
        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] int limit = 10)
        {
            var result = await _friendService.GetSuggestionsAsync(CurrentUserId, limit);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // GET api/friends/summary/{userId}
        [HttpGet("summary/{userId:guid}")]
        public async Task<IActionResult> GetSocialSummary(Guid userId)
        {
            var result = await _friendService.GetSocialSummaryAsync(userId, CurrentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ─── Friend Requests ────────────────────────────────────────────────────

        // POST api/friends/requests
        [HttpPost("requests")]
        public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto dto)
        {
            var result = await _requestService.SendRequestAsync(CurrentUserId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PUT api/friends/requests/{requestId}/accept
        [HttpPut("requests/{requestId:guid}/accept")]
        public async Task<IActionResult> AcceptRequest(Guid requestId)
        {
            var result = await _requestService.AcceptRequestAsync(requestId, CurrentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // PUT api/friends/requests/{requestId}/decline
        [HttpPut("requests/{requestId:guid}/decline")]
        public async Task<IActionResult> DeclineRequest(Guid requestId)
        {
            var result = await _requestService.DeclineRequestAsync(requestId, CurrentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // DELETE api/friends/requests/{requestId}
        [HttpDelete("requests/{requestId:guid}")]
        public async Task<IActionResult> CancelRequest(Guid requestId)
        {
            var result = await _requestService.CancelRequestAsync(requestId, CurrentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // GET api/friends/requests/sent
        [HttpGet("requests/sent")]
        public async Task<IActionResult> GetSentRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _requestService.GetSentRequestsAsync(CurrentUserId, page, pageSize);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // GET api/friends/requests/received
        [HttpGet("requests/received")]
        public async Task<IActionResult> GetReceivedRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _requestService.GetReceivedRequestsAsync(CurrentUserId, page, pageSize);
    
            var jsonString = JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
            
            // Dùng _logger in ra thay vì Console để log sạch đẹp
            _logger.LogInformation("Data bên trong Result:\n{Data}", jsonString);

            return result.Success ? Ok(result) : BadRequest(result);
        }
    }

    // ─── Follow Controller ──────────────────────────────────────────────────────

    [ApiController]
    [Authorize]
    [Route("api/follows")]
    public class FollowController : ControllerBase
    {
        private readonly IFollowService _followService;
        private Guid CurrentUserId
        {
            get
            {
                var userIdClaim =
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                    User.FindFirstValue("sub");

                return Guid.Parse(userIdClaim!);
            }
        }

        public FollowController(IFollowService followService) => _followService = followService;

        // POST api/follows/{followeeId}
        [HttpPost("{followeeId:guid}")]
        public async Task<IActionResult> Follow(Guid followeeId)
        {
            var result = await _followService.FollowAsync(CurrentUserId, followeeId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // DELETE api/follows/{followeeId}
        [HttpDelete("{followeeId:guid}")]
        public async Task<IActionResult> Unfollow(Guid followeeId)
        {
            var result = await _followService.UnfollowAsync(CurrentUserId, followeeId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // GET api/follows/followers/{userId}
        [HttpGet("followers/{userId:guid}")]
        public async Task<IActionResult> GetFollowers(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _followService.GetFollowersAsync(userId, page, pageSize);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // GET api/follows/following/{userId}
        [HttpGet("following/{userId:guid}")]
        public async Task<IActionResult> GetFollowing(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _followService.GetFollowingAsync(userId, page, pageSize);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }

    // ─── Block Controller ───────────────────────────────────────────────────────

    [ApiController]
    [Authorize]
    [Route("api/blocks")]
    public class BlockController : ControllerBase
    {
        private readonly IBlockService _blockService;
        private Guid CurrentUserId
        {
            get
            {
                var userIdClaim =
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                    User.FindFirstValue("sub");

                return Guid.Parse(userIdClaim!);
            }
        }

        public BlockController(IBlockService blockService) => _blockService = blockService;

        // POST api/blocks/{blockedId}
        [HttpPost("{blockedId:guid}")]
        public async Task<IActionResult> Block(Guid blockedId)
        {
            var result = await _blockService.BlockUserAsync(CurrentUserId, blockedId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // DELETE api/blocks/{blockedId}
        [HttpDelete("{blockedId:guid}")]
        public async Task<IActionResult> Unblock(Guid blockedId)
        {
            var result = await _blockService.UnblockUserAsync(CurrentUserId, blockedId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // GET api/blocks
        [HttpGet]
        public async Task<IActionResult> GetBlockedUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _blockService.GetBlockedUsersAsync(CurrentUserId, page, pageSize);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}