using System;

namespace Post.Domain.Entities
{
    /// <summary>
    /// A bookmark/save of a post by a user. Soft-deletable so a user can re-save
    /// after un-saving (mirrors PostLike behaviour).
    /// </summary>
    public class SavedPost
    {
        public Guid Id { get; private set; }
        public Guid PostId { get; private set; }
        public Guid UserId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        // Navigation
        public Post Post { get; private set; }

        private SavedPost() { }

        public static SavedPost Create(Guid postId, Guid userId)
        {
            return new SavedPost
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }
    }
}
