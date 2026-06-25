using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friend.Application.DTOs;
using Friend.Application.Interfaces;
using Friend.Domain.Entities;
using Friend.Domain.Interfaces;

namespace Friend.Application.Services
{
    // ─── FollowService ─────────────────────────────────────────────────────────

    public class FollowService : IFollowService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserProfileHttpClient _userClient;
        private readonly IMessagePublisher _publisher;

        public FollowService(
            IUnitOfWork unitOfWork,
            IUserProfileHttpClient userClient,
            IMessagePublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _userClient = userClient;
            _publisher = publisher;
        }

        public async Task<ApiResponse<bool>> FollowAsync(Guid followerId, Guid followeeId)
        {
            try
            {
                if (followerId == followeeId)
                    return ApiResponse<bool>.ErrorResponse("You cannot follow yourself.");

                if (await _unitOfWork.Blocks.IsBlockedAsync(followerId, followeeId))
                    return ApiResponse<bool>.ErrorResponse("Unable to follow this user.");

                var existing = await _unitOfWork.Follows.GetByUsersAsync(followerId, followeeId);
                if (existing != null)
                {
                    if (!existing.IsDeleted)
                    {
                        return ApiResponse<bool>.ErrorResponse("You are already following this user.");
                    }

                    existing.Restore();
                    await _unitOfWork.Follows.UpdateAsync(existing);
                }
                else
                {
                    var follow = Follow.Create(followerId, followeeId);
                    await _unitOfWork.Follows.AddAsync(follow);
                }

                await _unitOfWork.SaveChangesAsync();

                await _publisher.PublishUserFollowedAsync(followerId, followeeId);

                return ApiResponse<bool>.SuccessResponse(true, "Followed successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error following user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UnfollowAsync(Guid followerId, Guid followeeId)
        {
            try
            {
                var follow = await _unitOfWork.Follows.GetByUsersAsync(followerId, followeeId);
                if (follow == null || follow.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("You are not following this user.");

                follow.Unfollow();
                await _unitOfWork.Follows.UpdateAsync(follow);
                await _unitOfWork.SaveChangesAsync();

                // await _publisher.PublishUserUnfollowedAsync(followerId, followeeId);

                return ApiResponse<bool>.SuccessResponse(true, "Unfollowed successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error unfollowing user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PaginatedResponse<FollowDto>>> GetFollowersAsync(Guid userId, int page, int pageSize)
        {
            try
            {
                var follows    = await _unitOfWork.Follows.GetFollowersAsync(userId, page, pageSize);
                var totalCount = await _unitOfWork.Follows.GetFollowersCountAsync(userId);
                var userIds    = follows.Select(f => f.FollowerId).ToList();
                var profiles   = await _userClient.GetUserProfilesAsync(userIds);

                var dtos = follows.Select(f =>
                {
                    var profile = profiles.FirstOrDefault(p => p.UserId == f.FollowerId)
                                  ?? new UserProfileDto { UserId = f.FollowerId, Name = "Unknown", UserName = "unknown" };
                    return new FollowDto { Id = f.Id, User = profile, CreatedAt = f.CreatedAt };
                }).ToList();

                return ApiResponse<PaginatedResponse<FollowDto>>.SuccessResponse(new PaginatedResponse<FollowDto>
                {
                    Items = dtos, Page = page, PageSize = pageSize, TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<PaginatedResponse<FollowDto>>.ErrorResponse($"Error retrieving followers: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PaginatedResponse<FollowDto>>> GetFollowingAsync(Guid userId, int page, int pageSize)
        {
            try
            {
                var follows    = await _unitOfWork.Follows.GetFollowingAsync(userId, page, pageSize);
                var totalCount = await _unitOfWork.Follows.GetFollowingCountAsync(userId);
                var userIds    = follows.Select(f => f.FolloweeId).ToList();
                var profiles   = await _userClient.GetUserProfilesAsync(userIds);

                var dtos = follows.Select(f =>
                {
                    var profile = profiles.FirstOrDefault(p => p.UserId == f.FolloweeId)
                                  ?? new UserProfileDto { UserId = f.FolloweeId, Name = "Unknown", UserName = "unknown" };
                    return new FollowDto { Id = f.Id, User = profile, CreatedAt = f.CreatedAt };
                }).ToList();

                return ApiResponse<PaginatedResponse<FollowDto>>.SuccessResponse(new PaginatedResponse<FollowDto>
                {
                    Items = dtos, Page = page, PageSize = pageSize, TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<PaginatedResponse<FollowDto>>.ErrorResponse($"Error retrieving following: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> IsFollowingAsync(Guid followerId, Guid followeeId)
        {
            try
            {
                var result = await _unitOfWork.Follows.IsFollowingAsync(followerId, followeeId);
                return ApiResponse<bool>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error checking follow status: {ex.Message}");
            }
        }
    }

    // ─── BlockService ──────────────────────────────────────────────────────────

    public class BlockService : IBlockService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserProfileHttpClient _userClient;
        private readonly IMessagePublisher _publisher;

        public BlockService(
            IUnitOfWork unitOfWork,
            IUserProfileHttpClient userClient,
            IMessagePublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _userClient = userClient;
            _publisher = publisher;
        }

        public async Task<ApiResponse<bool>> BlockUserAsync(Guid blockerId, Guid blockedId)
        {
            try
            {
                if (blockerId == blockedId)
                    return ApiResponse<bool>.ErrorResponse("You cannot block yourself.");

                var existing = await _unitOfWork.Blocks.GetByUsersAsync(blockerId, blockedId);
                if (existing != null && !existing.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("User is already blocked.");

                var block = Block.Create(blockerId, blockedId);
                await _unitOfWork.Blocks.AddAsync(block);

                // Also remove any existing friendship silently
                var friendship = await _unitOfWork.Friendships.GetByUsersAsync(blockerId, blockedId);
                if (friendship != null && !friendship.IsDeleted)
                {
                    friendship.Unfriend();
                    await _unitOfWork.Friendships.UpdateAsync(friendship);
                }

                // Cancel any pending friend requests between them
                var pendingRequest = await _unitOfWork.FriendRequests.GetPendingBetweenAsync(blockerId, blockedId);
                if (pendingRequest != null)
                {
                    pendingRequest.Cancel();
                    await _unitOfWork.FriendRequests.UpdateAsync(pendingRequest);
                }

                // Remove follows in both directions
                var followA = await _unitOfWork.Follows.GetByUsersAsync(blockerId, blockedId);
                if (followA != null && !followA.IsDeleted) { followA.Unfollow(); await _unitOfWork.Follows.UpdateAsync(followA); }

                var followB = await _unitOfWork.Follows.GetByUsersAsync(blockedId, blockerId);
                if (followB != null && !followB.IsDeleted) { followB.Unfollow(); await _unitOfWork.Follows.UpdateAsync(followB); }

                await _unitOfWork.SaveChangesAsync();
                // await _publisher.PublishUserBlockedAsync(blockerId, blockedId);

                return ApiResponse<bool>.SuccessResponse(true, "User blocked successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error blocking user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UnblockUserAsync(Guid blockerId, Guid blockedId)
        {
            try
            {
                var block = await _unitOfWork.Blocks.GetByUsersAsync(blockerId, blockedId);
                if (block == null || block.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("Block not found.");

                block.Unblock();
                await _unitOfWork.Blocks.UpdateAsync(block);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true, "User unblocked successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error unblocking user: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PaginatedResponse<BlockDto>>> GetBlockedUsersAsync(Guid blockerId, int page, int pageSize)
        {
            try
            {
                var blocks     = await _unitOfWork.Blocks.GetBlockedByUserAsync(blockerId, page, pageSize);
                var totalCount = await _unitOfWork.Blocks.GetBlockedByUserCountAsync(blockerId);
                var userIds    = blocks.Select(b => b.BlockedId).ToList();
                var profiles   = await _userClient.GetUserProfilesAsync(userIds);

                var dtos = blocks.Select(b =>
                {
                    var profile = profiles.FirstOrDefault(p => p.UserId == b.BlockedId)
                                  ?? new UserProfileDto { UserId = b.BlockedId, Name = "Unknown", UserName = "unknown" };
                    return new BlockDto { Id = b.Id, BlockedUser = profile, CreatedAt = b.CreatedAt };
                }).ToList();

                return ApiResponse<PaginatedResponse<BlockDto>>.SuccessResponse(new PaginatedResponse<BlockDto>
                {
                    Items = dtos, Page = page, PageSize = pageSize, TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<PaginatedResponse<BlockDto>>.ErrorResponse($"Error retrieving blocked users: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> IsBlockedAsync(Guid userA, Guid userB)
        {
            try
            {
                var result = await _unitOfWork.Blocks.IsBlockedAsync(userA, userB);
                return ApiResponse<bool>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error checking block status: {ex.Message}");
            }
        }
    }
}