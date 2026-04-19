using System;

namespace Friend.Domain.Entities
{
    /// <summary>
    /// Represents a unidirectional follow: FollowerId follows FolloweeId.
    /// Independent from friendship — users can follow without being friends.
    /// </summary>
    public class Follow
    {
        public Guid Id { get; private set; }
        public Guid FollowerId { get; private set; }   // the one who follows
        public Guid FolloweeId { get; private set; }   // the one being followed
        public DateTime CreatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        private Follow() { }

        public static Follow Create(Guid followerId, Guid followeeId)
        {
            if (followerId == followeeId)
                throw new ArgumentException("A user cannot follow themselves.");

            return new Follow
            {
                Id = Guid.NewGuid(),
                FollowerId = followerId,
                FolloweeId = followeeId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public void Unfollow()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
        }
    }

    /// <summary>
    /// Represents a block: BlockerId blocks BlockedId.
    /// When a block exists, follow / friend interactions are prevented in both directions.
    /// </summary>
    public class Block
    {
        public Guid Id { get; private set; }
        public Guid BlockerId { get; private set; }
        public Guid BlockedId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        private Block() { }

        public static Block Create(Guid blockerId, Guid blockedId)
        {
            if (blockerId == blockedId)
                throw new ArgumentException("A user cannot block themselves.");

            return new Block
            {
                Id = Guid.NewGuid(),
                BlockerId = blockerId,
                BlockedId = blockedId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public void Unblock()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }
    }
}