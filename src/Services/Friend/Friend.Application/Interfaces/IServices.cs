using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friend.Application.DTOs;
namespace Friend.Application.Interfaces
{
    public interface IFriendService
    {
        Task<ApiResponse<PaginatedResponse<FriendshipDto>>> GetFriendsAsync(Guid userId, int page, int pageSize);
        Task<ApiResponse<List<Guid>>> GetFriendIdsAsync(Guid userId);
        Task<ApiResponse<bool>> UnfriendAsync(Guid userId, Guid targetUserId);
        Task<ApiResponse<UserSocialSummaryDto>> GetSocialSummaryAsync(Guid targetUserId, Guid? currentUserId = null);
        Task<ApiResponse<List<FriendSuggestionDto>>> GetSuggestionsAsync(Guid userId, int limit);
    }

    public interface IFriendRequestService
    {
        Task<ApiResponse<FriendRequestDto>> SendRequestAsync(Guid senderId, SendFriendRequestDto dto);
        Task<ApiResponse<FriendRequestDto>> AcceptRequestAsync(Guid requestId, Guid currentUserId);
        Task<ApiResponse<FriendRequestDto>> DeclineRequestAsync(Guid requestId, Guid currentUserId);
        Task<ApiResponse<bool>> CancelRequestAsync(Guid requestId, Guid currentUserId);
        Task<ApiResponse<PaginatedResponse<FriendRequestDto>>> GetSentRequestsAsync(Guid userId, int page, int pageSize);
        Task<ApiResponse<PaginatedResponse<FriendRequestDto>>> GetReceivedRequestsAsync(Guid userId, int page, int pageSize);
    }

    public interface IFollowService
    {
        Task<ApiResponse<bool>> FollowAsync(Guid followerId, Guid followeeId);
        Task<ApiResponse<bool>> UnfollowAsync(Guid followerId, Guid followeeId);
        Task<ApiResponse<PaginatedResponse<FollowDto>>> GetFollowersAsync(Guid userId, int page, int pageSize);
        Task<ApiResponse<PaginatedResponse<FollowDto>>> GetFollowingAsync(Guid userId, int page, int pageSize);
        Task<ApiResponse<bool>> IsFollowingAsync(Guid followerId, Guid followeeId);
    }

    public interface IBlockService
    {
        Task<ApiResponse<bool>> BlockUserAsync(Guid blockerId, Guid blockedId);
        Task<ApiResponse<bool>> UnblockUserAsync(Guid blockerId, Guid blockedId);
        Task<ApiResponse<PaginatedResponse<BlockDto>>> GetBlockedUsersAsync(Guid blockerId, int page, int pageSize);
        Task<ApiResponse<bool>> IsBlockedAsync(Guid userA, Guid userB);
    }

    // ─── Cross-service clients ─────────────────────────────────────────────────

    public interface IUserProfileHttpClient
    {
        Task<UserProfileDto?> GetUserProfileAsync(Guid userId);
        Task<List<UserProfileDto>> GetUserProfilesAsync(List<Guid> userIds);
    }

    // ─── Message publisher ─────────────────────────────────────────────────────

    public interface IMessagePublisher
    {
        Task PublishFriendRequestSentAsync(Guid requestId, Guid senderId, Guid receiverId);
        Task PublishFriendRequestAcceptedAsync(Guid requestId, Guid senderId, Guid receiverId);
        Task PublishFriendRequestDeclinedAsync(Guid requestId, Guid senderId, Guid receiverId);
        Task PublishFriendRemovedAsync(Guid userId, Guid targetUserId);
        Task PublishUserFollowedAsync(Guid followerId, Guid followeeId);
        Task PublishUserUnfollowedAsync(Guid followerId, Guid followeeId);
        Task PublishUserBlockedAsync(Guid blockerId, Guid blockedId);
    }
}