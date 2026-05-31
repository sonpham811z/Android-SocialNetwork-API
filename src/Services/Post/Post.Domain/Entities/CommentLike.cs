using System;

namespace Post.Domain.Entities
{
    public class CommentLike
    {
        public Guid Id { get; private set; }
        public Guid CommentId { get; private set; }
        public Guid UserId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        public Comment Comment { get; private set; }

        private CommentLike() { }

        public static CommentLike Create(Guid commentId, Guid userId)
        {
            return new CommentLike
            {
                Id = Guid.NewGuid(),
                CommentId = commentId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public void RestoreLike()
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
