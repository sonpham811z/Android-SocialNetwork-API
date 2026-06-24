using System;
using Post.Domain.Entities;

namespace Post.Application.DTOs
{
    public class BoardPostDto
    {
        public Guid Id { get; set; }
        public string Tag { get; set; }           // "hỏibài", "timeline", ...
        public string Content { get; set; }
        public bool IsAnonymous { get; set; }
        public string? AuthorId { get; set; }     // null nếu ẩn danh
        public string? AuthorName { get; set; }
        public string? AuthorAvatar { get; set; }
        public int UpvotesCount { get; set; }
        public int DownvotesCount { get; set; }
        public int CommentsCount { get; set; }
        public int NetVotes { get; set; }
        public string? CurrentUserVote { get; set; } // "up" | "down" | null
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
    }

    public class CreateBoardPostDto
    {
        public string Tag { get; set; }
        public string Content { get; set; }
        public bool IsAnonymous { get; set; }
    }

    public class VoteBoardPostDto
    {
        public string VoteType { get; set; }  // "up" | "down"
    }

    public class BoardCommentDto
    {
        public Guid Id { get; set; }
        public Guid BoardPostId { get; set; }
        public bool IsAnonymous { get; set; }
        public string? AuthorId { get; set; }      // null khi ẩn danh
        public string? AuthorName { get; set; }
        public string? AuthorAvatar { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
        public bool IsMine { get; set; }            // người dùng hiện tại có thể xóa
    }

    public class CreateBoardCommentDto
    {
        public string Content { get; set; }
        public bool IsAnonymous { get; set; } = true;
    }
}
