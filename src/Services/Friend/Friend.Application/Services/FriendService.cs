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

        public FriendService(
            IUnitOfWork unitOfWork,
            IUserProfileHttpClient userClient,
            IMessagePublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _userClient = userClient;
            _publisher = publisher;
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
                    var profile  = profiles.FirstOrDefault(p => p.UserId == friendId)
                                   ?? new UserProfileDto { UserId = friendId, Name = "Unknown", UserName = "unknown" };
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
                var ids = await _unitOfWork.Friendships.GetFriendIdsAsync(userId);
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

                // await _publisher.PublishFriendRemovedAsync(userId, targetUserId);

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

        public async Task<ApiResponse<List<FriendSuggestionDto>>> GetSuggestionsAsync(Guid userId, int limit)
        {
            try
            {
                if (limit <= 0) limit = 10;

                var myFriendIds = await _unitOfWork.Friendships.GetFriendIdsAsync(userId);

                // People we should never suggest: self, existing friends,
                // anyone with a pending request, and anyone blocked (either direction).
                var exclude = new HashSet<Guid>(myFriendIds) { userId };
                foreach (var id in await _unitOfWork.FriendRequests.GetPendingPartnerIdsAsync(userId))
                    exclude.Add(id);
                foreach (var id in await _unitOfWork.Blocks.GetBlockRelatedUserIdsAsync(userId))
                    exclude.Add(id);

                // Friends-of-friends: a candidate's score = how many of my friends they are friends with.
                var mutualCounts = new Dictionary<Guid, int>();
                if (myFriendIds.Count > 0)
                {
                    var friendSet = new HashSet<Guid>(myFriendIds);
                    var edges = await _unitOfWork.Friendships.GetFriendshipsForUsersAsync(myFriendIds);

                    foreach (var edge in edges)
                    {
                        if (friendSet.Contains(edge.UserId1) && !exclude.Contains(edge.UserId2))
                            mutualCounts[edge.UserId2] = mutualCounts.GetValueOrDefault(edge.UserId2) + 1;
                        if (friendSet.Contains(edge.UserId2) && !exclude.Contains(edge.UserId1))
                            mutualCounts[edge.UserId1] = mutualCounts.GetValueOrDefault(edge.UserId1) + 1;
                    }
                }

                var topCandidates = mutualCounts
                    .OrderByDescending(kv => kv.Value)
                    .Take(limit)
                    .ToList();

                if (topCandidates.Count == 0)
                    return ApiResponse<List<FriendSuggestionDto>>.SuccessResponse(new List<FriendSuggestionDto>());

                var profiles = await _userClient.GetUserProfilesAsync(topCandidates.Select(c => c.Key).ToList());

                var suggestions = topCandidates
                    .Select(c =>
                    {
                        var profile = profiles.FirstOrDefault(p => p.UserId == c.Key);
                        return profile == null
                            ? null
                            : new FriendSuggestionDto { User = profile, MutualFriendsCount = c.Value };
                    })
                    .Where(s => s != null)
                    .Select(s => s!)
                    .ToList();

                return ApiResponse<List<FriendSuggestionDto>>.SuccessResponse(suggestions);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<FriendSuggestionDto>>.ErrorResponse($"Error retrieving suggestions: {ex.Message}");
            }
        }
    }
}