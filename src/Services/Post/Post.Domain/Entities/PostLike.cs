using System;

namespace Post.Domain.Entities
{
    public class PostLike
    {
        public Guid Id { get; private set; }
        public Guid PostId { get; private set; }
        public Guid UserId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }
        
        // Navigation
        public Post Post { get; private set; }
        
        private PostLike() { }
        
        public static PostLike Create(Guid postId, Guid userId)
        {
            return new PostLike
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }
        
        public void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }
    }
}