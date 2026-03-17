using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friend.Application.DTOs;
using Friend.Application.Interfaces;
using Friend.Domain.Interfaces;

namespace Friend.Application.Services
{
    public class FriendService : IFriendService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserProfileHttpClient _userClient;
        private readonly IMessagePublisher _publisher;
        private readonly ICacheService _cache;

        private static readonly TimeSpan FriendsCacheTtl = TimeSpan.FromMinutes(5);

        public FriendService(
            IUnitOfWork unitOfWork,
            IUserProfileHttpClient userClient,
            IMessagePublisher publisher,
            ICacheService cache)
        {
            _unitOfWork = unitOfWork;
            _userClient = userClient;
            _publisher = publisher;
            _cache = cache;
        }

        public async Task<ApiResponse<PaginatedResponse<FriendshipDto>>> GetFriendsAsync(Guid userId, int page, int pageSize)
        {
            try
            {
                var friendships = await _unitOfWork.Friendships.GetUserFriendsAsync(userId, page, pageSize);
                var totalCount  = await _unitOfWork.Friendships.GetFriendsCountAsync(userId);

                var friendIds = friendships.Select(f => f.GetOtherUserId(userId)).ToList();
                var profiles  = await _userClient.GetUserProfilesAsync(friendIds);

                var dtos = friendships.Select(f =>
                {
                    var friendId = f.GetOtherUserId(userId);
                    var profile  = profiles.FirstOrDefault(p => p.Id == friendId)
                                   ?? new UserProfileDto { Id = friendId, Name = "Unknown", UserName = "unknown" };
                    return new FriendshipDto { Id = f.Id, Friend = profile, CreatedAt = f.CreatedAt };
                }).ToList();

                return ApiResponse<PaginatedResponse<FriendshipDto>>.SuccessResponse(new PaginatedResponse<FriendshipDto>
                {
                    Items = dtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<PaginatedResponse<FriendshipDto>>.ErrorResponse($"Error retrieving friends: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<Guid>>> GetFriendIdsAsync(Guid userId)
        {
            try
            {
                var cacheKey = $"friends:{userId}:ids";
                var cached   = await _cache.GetAsync<List<Guid>>(cacheKey);
                if (cached != null)
                    return ApiResponse<List<Guid>>.SuccessResponse(cached);

                var ids = await _unitOfWork.Friendships.GetFriendIdsAsync(userId);
                await _cache.SetAsync(cacheKey, ids, FriendsCacheTtl);

                return ApiResponse<List<Guid>>.SuccessResponse(ids);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<Guid>>.ErrorResponse($"Error retrieving friend IDs: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UnfriendAsync(Guid userId, Guid targetUserId)
        {
            try
            {
                var friendship = await _unitOfWork.Friendships.GetByUsersAsync(userId, targetUserId);
                if (friendship == null || friendship.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("Friendship not found.");

                friendship.Unfriend();
                await _unitOfWork.Friendships.UpdateAsync(friendship);
                await _unitOfWork.SaveChangesAsync();

                await _cache.RemoveByPrefixAsync($"friends:{userId}");
                await _cache.RemoveByPrefixAsync($"friends:{targetUserId}");

                await _publisher.PublishFriendRemovedAsync(userId, targetUserId);

                return ApiResponse<bool>.SuccessResponse(true, "Unfriended successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error unfriending: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UserSocialSummaryDto>> GetSocialSummaryAsync(Guid targetUserId, Guid? currentUserId = null)
        {
            try
            {
                var friendsCount    = await _unitOfWork.Friendships.GetFriendsCountAsync(targetUserId);
                var followersCount  = await _unitOfWork.Follows.GetFollowersCountAsync(targetUserId);
                var followingCount  = await _unitOfWork.Follows.GetFollowingCountAsync(targetUserId);

                var summary = new UserSocialSummaryDto
                {
                    UserId = targetUserId,
                    FriendsCount = friendsCount,
                    FollowersCount = followersCount,
                    FollowingCount = followingCount
                };

                if (currentUserId.HasValue && currentUserId.Value != targetUserId)
                {
                    summary.IsFriend        = await _unitOfWork.Friendships.AreFriendsAsync(currentUserId.Value, targetUserId);
                    summary.IsFollowing     = await _unitOfWork.Follows.IsFollowingAsync(currentUserId.Value, targetUserId);
                    summary.IsBlocked       = await _unitOfWork.Blocks.IsBlockedAsync(currentUserId.Value, targetUserId);
                    var pending             = await _unitOfWork.FriendRequests.GetPendingBetweenAsync(currentUserId.Value, targetUserId);
                    summary.HasPendingRequest = pending != null;
                }

                return ApiResponse<UserSocialSummaryDto>.SuccessResponse(summary);
            }
            catch (Exception ex)
            {
                return ApiResponse<UserSocialSummaryDto>.ErrorResponse($"Error retrieving social summary: {ex.Message}");
            }
        }
    }
}