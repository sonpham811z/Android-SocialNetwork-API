using System;

namespace Post.Domain.Entities
{
    /// <summary>
    /// A comment on a Campus Board post. AuthorId is always stored (for ownership /
    /// delete), but is hidden from the DTO when IsAnonymous is true.
    /// </summary>
    public class BoardComment
    {
        public Guid Id { get; private set; }
        public Guid BoardPostId { get; private set; }
        public Guid AuthorId { get; private set; }
        public string Content { get; private set; }
        public bool IsAnonymous { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        // Navigation
        public BoardPost BoardPost { get; private set; }

        private BoardComment() { }

        public static BoardComment Create(Guid boardPostId, Guid authorId, string content, bool isAnonymous)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be empty");
            if (content.Length > 1000)
                throw new ArgumentException("Content cannot exceed 1000 characters");

            return new BoardComment
            {
                Id = Guid.NewGuid(),
                BoardPostId = boardPostId,
                AuthorId = authorId,
                Content = content.Trim(),
                IsAnonymous = isAnonymous,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }

        public bool CanBeDeletedBy(Guid userId) => AuthorId == userId && !IsDeleted;
    }
}
