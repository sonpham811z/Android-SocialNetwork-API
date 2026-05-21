using System;
using System.Collections.Generic;

namespace Post.Domain.Entities
{
    public class Story
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string? MediaUrl { get; private set; }
        public string? MediaPublicId { get; private set; }
        public string? ThumbnailUrl { get; private set; }
        public string? ThumbnailPublicId { get; private set; }
        public StoryMediaType MediaType { get; private set; }
        public int ViewsCount { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        private List<StoryView> _views = new();
        public IReadOnlyCollection<StoryView> Views => _views.AsReadOnly();

        private Story() { }

        public static Story CreateImageStory(Guid userId, string mediaUrl, string mediaPublicId)
        {
            if (string.IsNullOrWhiteSpace(mediaUrl))
                throw new ArgumentException("Media URL cannot be empty");

            var now = DateTime.UtcNow;
            return new Story
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MediaUrl = mediaUrl,
                MediaPublicId = mediaPublicId,
                MediaType = StoryMediaType.Image,
                ViewsCount = 0,
                CreatedAt = now,
                ExpiresAt = now.AddHours(24),
                IsDeleted = false
            };
        }

        public static Story CreateVideoStory(
            Guid userId,
            string mediaUrl,
            string mediaPublicId,
            string? thumbnailUrl,
            string? thumbnailPublicId)
        {
            if (string.IsNullOrWhiteSpace(mediaUrl))
                throw new ArgumentException("Media URL cannot be empty");

            var now = DateTime.UtcNow;
            return new Story
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MediaUrl = mediaUrl,
                MediaPublicId = mediaPublicId,
                ThumbnailUrl = thumbnailUrl,
                ThumbnailPublicId = thumbnailPublicId,
                MediaType = StoryMediaType.Video,
                ViewsCount = 0,
                CreatedAt = now,
                ExpiresAt = now.AddHours(24),
                IsDeleted = false
            };
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }

        public void IncrementViewsCount()
        {
            ViewsCount++;
        }

        public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;
        public bool CanBeDeletedBy(Guid userId) => UserId == userId && !IsDeleted;
    }

    public enum StoryMediaType
    {
        Image = 0,
        Video = 1
    }
}
