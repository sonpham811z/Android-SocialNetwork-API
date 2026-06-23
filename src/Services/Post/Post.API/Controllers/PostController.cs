using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Post.Application.DTOs;
using Post.Application.Interfaces;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Post.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : ControllerBase
    {
        private readonly IPostService _postService;
        private readonly ICommentService _commentService;

        public PostController(IPostService postService, ICommentService commentService)
        {
            _postService = postService;
            _commentService = commentService;
        }

        /// <summary>
        /// Get post by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PostDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPostById(Guid id)
        {
            var result = await _postService.GetPostByIdAsync(id);
            if (!result.Success)
                return NotFound(result);
            
            return Ok(result);
        }

        /// <summary>
        /// Get posts by user ID with pagination
        /// </summary>
        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<PostDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserPosts(
            Guid userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            Guid? currentUserId = null;
            if (User.Identity?.IsAuthenticated == true)
                currentUserId = GetCurrentUserId();

            var result = await _postService.GetUserPostsAsync(userId, page, pageSize, currentUserId);
            return Ok(result);
        }

        /// <summary>
        /// Get newsfeed for authenticated user
        /// </summary>
        [Authorize]
        [HttpGet("feed")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<PostDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeed(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            Console.WriteLine("Getting feed for user.............................................");
            var userId = GetCurrentUserId();
            

            var result = await _postService.GetFeedAsync(userId, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Create a text post
        /// </summary>
        [Authorize]
        [HttpPost("text")]
        [ProducesResponseType(typeof(ApiResponse<PostDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTextPost([FromBody] CreateTextPostDto dto)
        {
            var userId = GetCurrentUserId();

            var result = await _postService.CreateTextPostAsync(userId, dto);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetPostById), new { id = result.Data.Id }, result);
        }

        /// <summary>
        /// Create an image post
        /// </summary>
        [Authorize]
        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<PostDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateImagePost([FromForm] CreateImagePostDto dto, IFormFile image)
        {
            var userId = GetCurrentUserId();

            if (dto.Content == null || image == null)
                return BadRequest(ApiResponse<PostDto>.ErrorResponse("Media file is required"));

            var result = await _postService.CreateImagePostAsync(userId, dto, image);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetPostById), new { id = result.Data.Id }, result);
        }

        /// <summary>
        /// Create a voice/audio post
        /// </summary>
        [Authorize]
        [HttpPost("voice")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<PostDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateVoicePost([FromForm] CreateVoicePostDto dto, IFormFile audio)
        {
            var userId = GetCurrentUserId();

            if (dto.Content == null || audio == null)
                return BadRequest(ApiResponse<PostDto>.ErrorResponse("media file is required"));

            var result = await _postService.CreateVoicePostAsync(userId, dto, audio);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetPostById), new { id = result.Data.Id }, result);
        }

        /// <summary>
        /// Create a video post
        /// </summary>
        [Authorize]
        [HttpPost("video")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<PostDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateVideoPost([FromForm] CreateVideoPostDto dto, IFormFile video)
        {
            var userId = GetCurrentUserId();

            if (dto.Content == null || video == null)
                return BadRequest(ApiResponse<PostDto>.ErrorResponse("Media file is required"));

            var result = await _postService.CreateVideoPostAsync(userId, dto, video);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetPostById), new { id = result.Data.Id }, result);
        }

        /// <summary>
        /// Update a post
        /// </summary>
        [Authorize]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PostDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdatePostDto dto)
        {
            var userId = GetCurrentUserId();

            var result = await _postService.UpdatePostAsync(postId: id, userId, dto);

            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                    return NotFound(result);
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Delete a post (soft delete)
        /// </summary>
        [Authorize]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            var userId = GetCurrentUserId();

            var result = await _postService.DeletePostAsync(id, userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Like a post
        /// </summary>
        [Authorize]
        [HttpPost("{id:guid}/like")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LikePost(Guid id)
        {
            var userId = GetCurrentUserId();

            var result = await _postService.LikePostAsync(id, userId);
            return Ok(result);
        }

        /// <summary>
        /// Share a post to feed
        /// </summary>
        [Authorize]
        [HttpPost("{id:guid}/share")]
        [ProducesResponseType(typeof(ApiResponse<PostDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SharePost(Guid id, [FromBody] SharePostDto dto)
        {
            var userId = GetCurrentUserId();

            var result = await _postService.SharePostAsync(id, userId, dto);

            if (!result.Success)
            {
                if (result.Message.Contains("không tồn tại"))
                    return NotFound(result);
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetPostById), new { id = result.Data.Id }, result);
        }

        /// <summary>
        /// Unlike a post
        /// </summary>
        [Authorize]
        [HttpDelete("{id:guid}/like")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UnlikePost(Guid id)
        {
            var userId = GetCurrentUserId();

            var result = await _postService.UnlikePostAsync(id, userId);
            return Ok(result);
        }

        // ==================== COMMENT ENDPOINTS ====================

        /// <summary>
        /// Get comments for a post
        /// </summary>
        [HttpGet("{postId:guid}/comments")]
        [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPostComments(Guid postId)
        {
            Guid? currentUserId = null;
            if (User.Identity?.IsAuthenticated == true)
                currentUserId = GetCurrentUserId();

            var result = await _commentService.GetPostCommentsAsync(postId, currentUserId);
            return Ok(result);
        }

        /// <summary>
        /// Create a comment on a post
        /// </summary>
        [Authorize]
        [HttpPost("{postId:guid}/comments")]
        [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateComment(Guid postId, [FromBody] CreateCommentDto dto)
        {
            var userId = GetCurrentUserId();

            var result = await _commentService.CreateCommentAsync(postId, userId, dto);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetPostComments), new { postId }, result);
        }

        /// <summary>
        /// Update a comment
        /// </summary>
        [Authorize]
        [HttpPut("comments/{commentId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateComment(Guid commentId, [FromBody] UpdateCommentDto dto)
        {
            var userId = GetCurrentUserId();

            var result = await _commentService.UpdateCommentAsync(commentId, userId, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Delete a comment
        /// </summary>
        [Authorize]
        [HttpDelete("comments/{commentId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            var userId = GetCurrentUserId();

            var result = await _commentService.DeleteCommentAsync(commentId, userId);
            return Ok(result);
        }

        /// <summary>
        /// Like a comment
        /// </summary>
        [Authorize]
        [HttpPost("comments/{commentId:guid}/like")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LikeComment(Guid commentId)
        {
            var userId = GetCurrentUserId();
            var result = await _commentService.LikeCommentAsync(commentId, userId);
            return Ok(result);
        }

        /// <summary>
        /// Unlike a comment
        /// </summary>
        [Authorize]
        [HttpDelete("comments/{commentId:guid}/like")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UnlikeComment(Guid commentId)
        {
            var userId = GetCurrentUserId();
            var result = await _commentService.UnlikeCommentAsync(commentId, userId);
            return Ok(result);
        }

        // Helper method to get current user ID from JWT token
        private Guid GetCurrentUserId()
        {
           if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("User is not logged in."); 
            }

            var userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue("sub");

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("Invalid User ID in Token.");
        }
    }
}