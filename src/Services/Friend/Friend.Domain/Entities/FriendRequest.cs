using System;

namespace Friend.Domain.Entities
{
    public enum FriendRequestStatus
    {
        Pending = 0,
        Accepted = 1,
        Declined = 2,
        Cancelled = 3
    }

    public class FriendRequest
    {
        public Guid Id { get; private set; }
        public Guid SenderId { get; private set; }
        public Guid ReceiverId { get; private set; }
        public FriendRequestStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        private FriendRequest() { }

        public static FriendRequest Create(Guid senderId, Guid receiverId)
        {
            if (senderId == receiverId)
                throw new ArgumentException("Cannot send a friend request to yourself.");

            return new FriendRequest
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = receiverId,
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Accept()
        {
            if (Status != FriendRequestStatus.Pending)
                throw new InvalidOperationException("Only pending requests can be accepted.");

            Status = FriendRequestStatus.Accepted;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Decline()
        {
            if (Status != FriendRequestStatus.Pending)
                throw new InvalidOperationException("Only pending requests can be declined.");

            Status = FriendRequestStatus.Declined;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status != FriendRequestStatus.Pending)
                throw new InvalidOperationException("Only pending requests can be cancelled.");

            Status = FriendRequestStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsPending => Status == FriendRequestStatus.Pending;
        public bool CanBeManagedBy(Guid userId) => ReceiverId == userId;
        public bool CanBeCancelledBy(Guid userId) => SenderId == userId;
    }
}