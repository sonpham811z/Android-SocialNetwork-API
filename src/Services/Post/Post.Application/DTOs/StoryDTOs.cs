using System;
using System.Collections.Generic;

namespace Post.Application.DTOs
{
    public class StoryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public UserProfileDto User { get; set; } = null!;
        public string? MediaUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string MediaType { get; set; } = "Image";
        public int ViewsCount { get; set; }
        public bool IsViewedByCurrentUser { get; set; }
        public bool IsOwner { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class StoryFeedItemDto
    {
        public UserProfileDto User { get; set; } = null!;
        public List<StoryDto> Stories { get; set; } = new();
        public bool HasUnseenStories { get; set; }
    }

    public class StoryViewerDto
    {
        public Guid ViewerId { get; set; }
        public UserProfileDto User { get; set; } = null!;
        public DateTime ViewedAt { get; set; }
    }
}
