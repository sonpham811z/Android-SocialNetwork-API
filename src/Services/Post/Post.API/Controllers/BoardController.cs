using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Post.Application.DTOs;
using Post.Application.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Post.API.Controllers
{
    [ApiController]
    [Route("api/board")]
    public class BoardController : ControllerBase
    {
        private readonly IBoardService _boardService;

        public BoardController(IBoardService boardService)
        {
            _boardService = boardService;
        }

        /// <summary>GET /api/board?tag=hoibai&amp;sort=hot&amp;page=1&amp;pageSize=20</summary>
        [HttpGet]
        public async Task<IActionResult> GetPosts(
            [FromQuery] string? tag      = null,
            [FromQuery] string  sort     = "hot",
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 20)
        {
            Guid? currentUserId = null;
            if (User.Identity?.IsAuthenticated == true)
                currentUserId = GetCurrentUserId();

            var result = await _boardService.GetPostsAsync(tag, sort, page, pageSize, currentUserId);
            return Ok(result);
        }

        /// <summary>POST /api/board — tạo bài viết</summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreateBoardPostDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _boardService.CreatePostAsync(userId, dto);
            if (!result.Success) return BadRequest(result);
            return CreatedAtAction(nameof(GetPosts), result);
        }

        /// <summary>POST /api/board/{id}/vote — upvote hoặc downvote</summary>
        [Authorize]
        [HttpPost("{id:guid}/vote")]
        public async Task<IActionResult> Vote(Guid id, [FromBody] VoteBoardPostDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _boardService.VoteAsync(id, userId, dto.VoteType);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>DELETE /api/board/{id}/vote — xóa vote</summary>
        [Authorize]
        [HttpDelete("{id:guid}/vote")]
        public async Task<IActionResult> DeleteVote(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _boardService.DeleteVoteAsync(id, userId);
            return Ok(result);
        }

        /// <summary>DELETE /api/board/{id} — xóa bài viết của mình</summary>
        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            var userId = GetCurrentUserId();
            var result = await _boardService.DeletePostAsync(id, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>GET /api/board/{id}/comments — danh sách bình luận</summary>
        [HttpGet("{id:guid}/comments")]
        public async Task<IActionResult> GetComments(Guid id)
        {
            Guid? currentUserId = null;
            if (User.Identity?.IsAuthenticated == true)
                currentUserId = GetCurrentUserId();

            var result = await _boardService.GetCommentsAsync(id, currentUserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>POST /api/board/{id}/comments — thêm bình luận</summary>
        [Authorize]
        [HttpPost("{id:guid}/comments")]
        public async Task<IActionResult> AddComment(Guid id, [FromBody] CreateBoardCommentDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _boardService.AddCommentAsync(id, userId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>DELETE /api/board/comments/{commentId} — xóa bình luận của mình</summary>
        [Authorize]
        [HttpDelete("comments/{commentId:guid}")]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            var userId = GetCurrentUserId();
            var result = await _boardService.DeleteCommentAsync(commentId, userId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        private Guid GetCurrentUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? User.FindFirstValue("id");

            if (Guid.TryParse(sub, out var id)) return id;
            throw new UnauthorizedAccessException("Invalid token");
        }
    }
}
