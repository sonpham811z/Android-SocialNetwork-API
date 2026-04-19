using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friend.Domain.Entities;

namespace Friend.Domain.Interfaces
{
    public interface IFriendshipRepository
    {
        Task<Friendship?> GetByIdAsync(Guid id);
        Task<Friendship?> GetByUsersAsync(Guid userA, Guid userB);
        Task<Friendship?> GetByUsersIncludingDeletedAsync(Guid userA, Guid userB);
        Task<IEnumerable<Friendship>> GetUserFriendsAsync(Guid userId, int page, int pageSize);
        Task<int> GetFriendsCountAsync(Guid userId);
        Task<bool> AreFriendsAsync(Guid userA, Guid userB);

        /// <summary>Returns all friend Ids of a user (used for feed construction in Post service).</summary>
        Task<List<Guid>> GetFriendIdsAsync(Guid userId);

        Task<Friendship> AddAsync(Friendship friendship);
        Task UpdateAsync(Friendship friendship);
    }

    public interface IFriendRequestRepository
    {
        Task<FriendRequest?> GetByIdAsync(Guid id);

        /// <summary>Returns pending request between two users regardless of direction.</summary>
        Task<FriendRequest?> GetPendingBetweenAsync(Guid userA, Guid userB);

        Task<IEnumerable<FriendRequest>> GetSentRequestsAsync(Guid senderId, int page, int pageSize);
        Task<IEnumerable<FriendRequest>> GetReceivedRequestsAsync(Guid receiverId, int page, int pageSize);
        Task<int> GetSentRequestsCountAsync(Guid senderId);
        Task<int> GetReceivedRequestsCountAsync(Guid receiverId);
        Task<int> GetPendingReceivedCountAsync(Guid userId);

        Task<FriendRequest> AddAsync(FriendRequest request);
        Task UpdateAsync(FriendRequest request);
    }

    public interface IFollowRepository
    {
        Task<Follow?> GetByIdAsync(Guid id);
        Task<Follow?> GetByUsersAsync(Guid followerId, Guid followeeId);
        Task<IEnumerable<Follow>> GetFollowersAsync(Guid followeeId, int page, int pageSize);
        Task<IEnumerable<Follow>> GetFollowingAsync(Guid followerId, int page, int pageSize);
        Task<int> GetFollowersCountAsync(Guid userId);
        Task<int> GetFollowingCountAsync(Guid userId);
        Task<bool> IsFollowingAsync(Guid followerId, Guid followeeId);
        Task<List<Guid>> GetFollowingIdsAsync(Guid followerId);

        Task<Follow> AddAsync(Follow follow);
        Task UpdateAsync(Follow follow);
    }

    public interface IBlockRepository
    {
        Task<Block?> GetByIdAsync(Guid id);
        Task<Block?> GetByUsersAsync(Guid blockerId, Guid blockedId);
        Task<IEnumerable<Block>> GetBlockedByUserAsync(Guid blockerId, int page, int pageSize);
        Task<int> GetBlockedByUserCountAsync(Guid blockerId);

        /// <summary>True if either direction of block exists between the two users.</summary>
        Task<bool> IsBlockedAsync(Guid userA, Guid userB);

        Task<Block> AddAsync(Block block);
        Task UpdateAsync(Block block);
    }

    public interface IUnitOfWork : IDisposable
    {
        IFriendshipRepository Friendships { get; }
        IFriendRequestRepository FriendRequests { get; }
        IFollowRepository Follows { get; }
        IBlockRepository Blocks { get; }

        Task SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}