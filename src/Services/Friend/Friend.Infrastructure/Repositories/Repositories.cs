using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friend.Domain.Entities;
using Friend.Domain.Interfaces;
using Friend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Friend.Infrastructure.Repositories
{
    public class FriendshipRepository : IFriendshipRepository
    {
        private readonly FriendDbContext _ctx;
        public FriendshipRepository(FriendDbContext ctx) => _ctx = ctx;

        public Task<Friendship?> GetByIdAsync(Guid id) =>
            _ctx.Friendships.FirstOrDefaultAsync(f => f.Id == id);

        public Task<Friendship?> GetByUsersAsync(Guid userA, Guid userB)
        {
            var (u1, u2) = userA < userB ? (userA, userB) : (userB, userA);
            return _ctx.Friendships.FirstOrDefaultAsync(f => f.UserId1 == u1 && f.UserId2 == u2);
        }

        public async Task<IEnumerable<Friendship>> GetUserFriendsAsync(Guid userId, int page, int pageSize) =>
            await _ctx.Friendships
                .Where(f => f.UserId1 == userId || f.UserId2 == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

        public Task<int> GetFriendsCountAsync(Guid userId) =>
            _ctx.Friendships.CountAsync(f => f.UserId1 == userId || f.UserId2 == userId);

        public Task<bool> AreFriendsAsync(Guid userA, Guid userB)
        {
            var (u1, u2) = userA < userB ? (userA, userB) : (userB, userA);
            return _ctx.Friendships.AnyAsync(f => f.UserId1 == u1 && f.UserId2 == u2);
        }

        public async Task<List<Guid>> GetFriendIdsAsync(Guid userId)
        {
            var friendships = await _ctx.Friendships
                .Where(f => f.UserId1 == userId || f.UserId2 == userId)
                .ToListAsync();

            return friendships.Select(f => f.GetOtherUserId(userId)).ToList();
        }

        public async Task<Friendship> AddAsync(Friendship friendship)
        {
            await _ctx.Friendships.AddAsync(friendship);
            return friendship;
        }

        public Task UpdateAsync(Friendship friendship)
        {
            _ctx.Friendships.Update(friendship);
            return Task.CompletedTask;
        }
    }

    public class FriendRequestRepository : IFriendRequestRepository
    {
        private readonly FriendDbContext _ctx;
        public FriendRequestRepository(FriendDbContext ctx) => _ctx = ctx;

        public Task<FriendRequest?> GetByIdAsync(Guid id) =>
            _ctx.FriendRequests.FirstOrDefaultAsync(r => r.Id == id);

        public Task<FriendRequest?> GetPendingBetweenAsync(Guid userA, Guid userB) =>
            _ctx.FriendRequests.FirstOrDefaultAsync(r =>
                r.Status == FriendRequestStatus.Pending &&
                ((r.SenderId == userA && r.ReceiverId == userB) ||
                 (r.SenderId == userB && r.ReceiverId == userA)));

        public async Task<IEnumerable<FriendRequest>> GetSentRequestsAsync(Guid senderId, int page, int pageSize) =>
            await _ctx.FriendRequests
                .Where(r => r.SenderId == senderId && r.Status == FriendRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

        public async Task<IEnumerable<FriendRequest>> GetReceivedRequestsAsync(Guid receiverId, int page, int pageSize) =>
            await _ctx.FriendRequests
                .Where(r => r.ReceiverId == receiverId && r.Status == FriendRequestStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

        public Task<int> GetPendingReceivedCountAsync(Guid userId) =>
            _ctx.FriendRequests.CountAsync(r => r.ReceiverId == userId && r.Status == FriendRequestStatus.Pending);

        public async Task<FriendRequest> AddAsync(FriendRequest request)
        {
            await _ctx.FriendRequests.AddAsync(request);
            return request;
        }

        public Task UpdateAsync(FriendRequest request)
        {
            _ctx.FriendRequests.Update(request);
            return Task.CompletedTask;
        }
    }

    public class FollowRepository : IFollowRepository
    {
        private readonly FriendDbContext _ctx;
        public FollowRepository(FriendDbContext ctx) => _ctx = ctx;

        public Task<Follow?> GetByIdAsync(Guid id) =>
            _ctx.Follows.FirstOrDefaultAsync(f => f.Id == id);

        public Task<Follow?> GetByUsersAsync(Guid followerId, Guid followeeId) =>
            _ctx.Follows.IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);

        public async Task<IEnumerable<Follow>> GetFollowersAsync(Guid followeeId, int page, int pageSize) =>
            await _ctx.Follows
                .Where(f => f.FolloweeId == followeeId)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

        public async Task<IEnumerable<Follow>> GetFollowingAsync(Guid followerId, int page, int pageSize) =>
            await _ctx.Follows
                .Where(f => f.FollowerId == followerId)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

        public Task<int> GetFollowersCountAsync(Guid userId) =>
            _ctx.Follows.CountAsync(f => f.FolloweeId == userId);

        public Task<int> GetFollowingCountAsync(Guid userId) =>
            _ctx.Follows.CountAsync(f => f.FollowerId == userId);

        public Task<bool> IsFollowingAsync(Guid followerId, Guid followeeId) =>
            _ctx.Follows.AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);

        public async Task<List<Guid>> GetFollowingIdsAsync(Guid followerId) =>
            await _ctx.Follows.Where(f => f.FollowerId == followerId)
                .Select(f => f.FolloweeId).ToListAsync();

        public async Task<Follow> AddAsync(Follow follow)
        {
            await _ctx.Follows.AddAsync(follow);
            return follow;
        }

        public Task UpdateAsync(Follow follow)
        {
            _ctx.Follows.Update(follow);
            return Task.CompletedTask;
        }
    }

    public class BlockRepository : IBlockRepository
    {
        private readonly FriendDbContext _ctx;
        public BlockRepository(FriendDbContext ctx) => _ctx = ctx;

        public Task<Block?> GetByIdAsync(Guid id) =>
            _ctx.Blocks.FirstOrDefaultAsync(b => b.Id == id);

        public Task<Block?> GetByUsersAsync(Guid blockerId, Guid blockedId) =>
            _ctx.Blocks.IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);

        public async Task<IEnumerable<Block>> GetBlockedByUserAsync(Guid blockerId, int page, int pageSize) =>
            await _ctx.Blocks
                .Where(b => b.BlockerId == blockerId)
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

        // Checks EITHER direction of block
        public Task<bool> IsBlockedAsync(Guid userA, Guid userB) =>
            _ctx.Blocks.AnyAsync(b =>
                (b.BlockerId == userA && b.BlockedId == userB) ||
                (b.BlockerId == userB && b.BlockedId == userA));

        public async Task<Block> AddAsync(Block block)
        {
            await _ctx.Blocks.AddAsync(block);
            return block;
        }

        public Task UpdateAsync(Block block)
        {
            _ctx.Blocks.Update(block);
            return Task.CompletedTask;
        }
    }
}