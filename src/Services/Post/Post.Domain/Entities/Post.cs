using System;
using System.Collections.Generic;
using System.Linq;

namespace Post.Domain.Entities
{
    public class Post
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string Content { get; private set; }
        public PostType Type { get; private set; }
        
        // Media properties
        public string? ImageUrl { get; private set; }
        public string? ImagePublicId { get; private set; }
        public string? AudioUrl { get; private set; }
        public string? AudioPublicId { get; private set; }
        public string? AudioDuration { get; private set; }
        public List<double>? Waveform { get; private set; }
        public string? VideoUrl { get; private set; }
        public string? VideoPublicId { get; private set; }
        public string? VideoThumbnailUrl { get; private set; }
        
        // Engagement
        public int LikesCount { get; private set; }
        public int CommentsCount { get; private set; }
        public int SharesCount { get; private set; }
        
        // Metadata
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        // Moderation: admin ẩn bài (tách biệt với IsDeleted do chủ post xoá)
        public bool IsHidden { get; private set; }

        // Privacy
        public PostVisibility Visibility { get; private set; }

        // Share reference
        public Guid? OriginalPostId { get; private set; }

        // Navigation properties
        private List<Comment> _comments = new List<Comment>();
        public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
        
        private List<PostLike> _likes = new List<PostLike>();
        public IReadOnlyCollection<PostLike> Likes => _likes.AsReadOnly();
        
        // Constructor for EF Core
        private Post() 
        { 
            Content = null!; 
        }
        
        // Factory methods
        public static Post CreateTextPost(Guid userId, string content, PostVisibility visibility = PostVisibility.Public)
        {
            ValidateContent(content);
            
            return new Post
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Content = content,
                Type = PostType.Text,
                Visibility = visibility,
                CreatedAt = DateTime.UtcNow,
                LikesCount = 0,
                CommentsCount = 0,
                SharesCount = 0,
                IsDeleted = false
            };
        }
        
        public static Post CreateImagePost(Guid userId, string content, string imageUrl, string imagePublicId, PostVisibility visibility = PostVisibility.Public)
        {
            ValidateContent(content);
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("Image URL cannot be empty for image post");
            
            return new Post
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Content = content,
                Type = PostType.Image,
                ImageUrl = imageUrl,
                ImagePublicId = imagePublicId,
                Visibility = visibility,
                CreatedAt = DateTime.UtcNow,
                LikesCount = 0,
                CommentsCount = 0,
                SharesCount = 0,
                IsDeleted = false
            };
        }
        
        public static Post CreateVoicePost(
            Guid userId, 
            string content, 
            string audioUrl,
            string audioPublicId,
            string audioDuration,
            List<double> waveform,
            PostVisibility visibility = PostVisibility.Public)
        {
            ValidateContent(content);
            if (string.IsNullOrWhiteSpace(audioUrl))
                throw new ArgumentException("Audio URL cannot be empty for voice post");
            
            return new Post
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Content = content,
                Type = PostType.Voice,
                AudioUrl = audioUrl,
                AudioPublicId = audioPublicId,
                AudioDuration = audioDuration,
                Waveform = waveform,
                Visibility = visibility,
                CreatedAt = DateTime.UtcNow,
                LikesCount = 0,
                CommentsCount = 0,
                SharesCount = 0,
                IsDeleted = false
            };
        }
        
        public static Post CreateVideoPost(
            Guid userId,
            string content,
            string videoUrl,
            string videoPublicId,
            string? thumbnailUrl,
            PostVisibility visibility = PostVisibility.Public)
        {
            ValidateContent(content);
            if (string.IsNullOrWhiteSpace(videoUrl))
                throw new ArgumentException("Video URL cannot be empty for video post");

            return new Post
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Content = content,
                Type = PostType.Video,
                VideoUrl = videoUrl,
                VideoPublicId = videoPublicId,
                VideoThumbnailUrl = thumbnailUrl,
                Visibility = visibility,
                CreatedAt = DateTime.UtcNow,
                LikesCount = 0,
                CommentsCount = 0,
                SharesCount = 0,
                IsDeleted = false
            };
        }

        public static Post CreateSharedPost(
            Guid userId,
            string content,
            PostVisibility visibility,
            Guid originalPostId)
        {
            if (content != null && content.Length > 5000)
                throw new ArgumentException("Post content cannot exceed 5000 characters");

            return new Post
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Content = content ?? string.Empty,
                Type = PostType.Text,
                Visibility = visibility,
                OriginalPostId = originalPostId,
                CreatedAt = DateTime.UtcNow,
                LikesCount = 0,
                CommentsCount = 0,
                SharesCount = 0,
                IsDeleted = false
            };
        }

        // Business logic methods
        public void UpdateContent(string newContent)
        {
            ValidateContent(newContent);
            Content = newContent;
            UpdatedAt = DateTime.UtcNow;
        }

        // ── Media editing ─────────────────────────────────────────────────────
        private void ClearAllMedia()
        {
            ImageUrl = null;
            ImagePublicId = null;
            AudioUrl = null;
            AudioPublicId = null;
            AudioDuration = null;
            Waveform = null;
            VideoUrl = null;
            VideoPublicId = null;
            VideoThumbnailUrl = null;
        }

        /// <summary>Gỡ toàn bộ media, chuyển bài về dạng Text.</summary>
        public void RemoveMedia()
        {
            ClearAllMedia();
            Type = PostType.Text;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetImageMedia(string imageUrl, string imagePublicId)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("Image URL cannot be empty for image post");
            ClearAllMedia();
            Type = PostType.Image;
            ImageUrl = imageUrl;
            ImagePublicId = imagePublicId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetVideoMedia(string videoUrl, string videoPublicId, string? thumbnailUrl)
        {
            if (string.IsNullOrWhiteSpace(videoUrl))
                throw new ArgumentException("Video URL cannot be empty for video post");
            ClearAllMedia();
            Type = PostType.Video;
            VideoUrl = videoUrl;
            VideoPublicId = videoPublicId;
            VideoThumbnailUrl = thumbnailUrl;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetVoiceMedia(string audioUrl, string audioPublicId, string audioDuration, List<double> waveform)
        {
            if (string.IsNullOrWhiteSpace(audioUrl))
                throw new ArgumentException("Audio URL cannot be empty for voice post");
            ClearAllMedia();
            Type = PostType.Voice;
            AudioUrl = audioUrl;
            AudioPublicId = audioPublicId;
            AudioDuration = audioDuration;
            Waveform = waveform;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public void AddComment(Comment comment)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot add comment to deleted post");
                
            _comments.Add(comment);
            CommentsCount++;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public void RemoveComment(Guid commentId)
        {
            var comment = _comments.FirstOrDefault(c => c.Id == commentId);
            if (comment != null)
            {
                comment.SoftDelete();
                CommentsCount--;
                UpdatedAt = DateTime.UtcNow;
            }
        }
        
        public void IncrementLikesCount()
        {
            LikesCount++;
            UpdatedAt = DateTime.UtcNow;
        }

        public void DecrementLikesCount()
        {
            if (LikesCount > 0) LikesCount--;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public void IncrementShareCount()
        {
            SharesCount++;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }
        
        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }

        // ── Moderation (admin) ────────────────────────────────────────────────
        public void Hide()
        {
            IsHidden = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Unhide()
        {
            IsHidden = false;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public void UpdateVisibility(PostVisibility visibility)
        {
            Visibility = visibility;
            UpdatedAt = DateTime.UtcNow;
        }
        
        private static void ValidateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Post content cannot be empty");
                
            if (content.Length > 5000)
                throw new ArgumentException("Post content cannot exceed 5000 characters");
        }
        
        public bool CanBeEditedBy(Guid userId) => UserId == userId && !IsDeleted;
        public bool CanBeDeletedBy(Guid userId) => UserId == userId && !IsDeleted;
    }
    
    public enum PostType
    {
        Text = 0,
        Image = 1,
        Voice = 2,
        Video = 3
    }
    
    public enum PostVisibility
    {
        Public = 0,
        Friends = 1,
        Private = 2
    }
}