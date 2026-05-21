using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Post.Application.Interfaces;

namespace Post.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoryController : ControllerBase
    {
        private readonly IStoryService _storyService;

        public StoryController(IStoryService storyService)
        {
            _storyService = storyService;
        }

        [HttpGet("feed")]
        [Authorize]
        public async Task<IActionResult> GetStoryFeed()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var result = await _storyService.GetStoryFeedAsync(currentUserId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetUserStories(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _storyService.GetUserStoriesAsync(userId, currentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetStory(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _storyService.GetStoryByIdAsync(id, currentUserId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost("image")]
        [Authorize]
        public async Task<IActionResult> CreateImageStory(IFormFile file)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var result = await _storyService.CreateImageStoryAsync(currentUserId.Value, file);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("video")]
        [Authorize]
        public async Task<IActionResult> CreateVideoStory(IFormFile file)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var result = await _storyService.CreateVideoStoryAsync(currentUserId.Value, file);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteStory(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var result = await _storyService.DeleteStoryAsync(id, currentUserId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/view")]
        [Authorize]
        public async Task<IActionResult> ViewStory(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var result = await _storyService.ViewStoryAsync(id, currentUserId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id:guid}/viewers")]
        [Authorize]
        public async Task<IActionResult> GetStoryViewers(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized();

            var result = await _storyService.GetStoryViewersAsync(id, currentUserId.Value);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
