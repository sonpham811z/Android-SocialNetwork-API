using System;

namespace Friend.Domain.Entities
{
    /// <summary>
    /// Represents a bidirectional friend relationship between two users.
    /// A Friendship is created when a FriendRequest is accepted.
    /// </summary>
    public class Friendship
    {
        public Guid Id { get; private set; }

        // Always store in canonical order: UserId1 < UserId2 (prevents duplicate pairs)
        public Guid UserId1 { get; private set; }
        public Guid UserId2 { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        private Friendship() { }

        public static Friendship Create(Guid userA, Guid userB)
        {
            // Canonical ordering prevents (A,B) and (B,A) duplicates
            var (u1, u2) = userA < userB ? (userA, userB) : (userB, userA);

            return new Friendship
            {
                Id = Guid.NewGuid(),
                UserId1 = u1,
                UserId2 = u2,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        /// <summary>Returns the other user's Id given one side of the friendship.</summary>
        public Guid GetOtherUserId(Guid userId) =>
            userId == UserId1 ? UserId2 : UserId1;

        public bool InvolveUser(Guid userId) =>
            UserId1 == userId || UserId2 == userId;

        public void Unfriend()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }
    }
}