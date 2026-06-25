using System;

namespace Post.Domain.Entities
{
    public enum VoteType { Up = 1, Down = -1 }

    public class BoardVote
    {
        public Guid Id { get; private set; }
        public Guid BoardPostId { get; private set; }
        public Guid UserId { get; private set; }
        public VoteType Type { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        public BoardPost BoardPost { get; private set; }

        private BoardVote() { }

        public static BoardVote Create(Guid boardPostId, Guid userId, VoteType type)
        {
            return new BoardVote
            {
                Id = Guid.NewGuid(),
                BoardPostId = boardPostId,
                UserId = userId,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public void ChangeType(VoteType newType) => Type = newType;

        public void Restore(VoteType type)
        {
            Type = type;
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
