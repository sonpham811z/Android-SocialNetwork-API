using System;

namespace Post.Domain.Entities
{
    public class Comment
    {
        public Guid Id { get; private set; }
        public Guid PostId { get; private set; }
        public Guid UserId { get; private set; }
        public string Content { get; private set; }

        //For nested comment (replies)
        public Guid? ParentCommentId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        public Post Post { get; private set; }
        public Comment? ParentComment { get; private set; }
        
        private Comment() {}

        public static Comment Create(Guid postId, Guid userId, string content, Guid? parentCommentId = null)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentException("Comment content cannot be empty");

            if (content.Length > 2000)
                throw new ArgumentException("Comment content cannot exceed 2000 characters");

            return new Comment
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                UserId = userId,
                Content = content,
                ParentCommentId = parentCommentId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public void UpdateContent(string newContent)
        {
            if (string.IsNullOrEmpty(newContent))
                throw new ArgumentException("comment content cannot be empty");
                 if (newContent.Length > 2000)
                throw new ArgumentException("Comment content cannot exceed 2000 characters");
            
            Content = newContent;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }
        
        public int LikesCount { get; private set; }

        public void IncrementLikesCount()
        {
            LikesCount++;
        }

        public void DecrementLikesCount()
        {
            if (LikesCount > 0) LikesCount--;
        }

        public bool CanBeEditedBy(Guid userId) => UserId == userId && !IsDeleted;
        public bool CanBeDeletedBy(Guid userId) => UserId == userId && !IsDeleted;
    }
}