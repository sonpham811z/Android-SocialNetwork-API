using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Friend.Domain.Entities;

namespace Friend.Application.DTOs
{
    // ─── Shared ────────────────────────────────────────────────────────────────

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string? message = null) =>
            new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> ErrorResponse(string message) =>
            new() { Success = false, Message = message };
    }

    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasNext => Page * PageSize < TotalCount;
    }

    // ─── User stub (fetched from User service) ─────────────────────────────────

    public class UserProfileDto
    {
        // Profile row PK from User service
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        // Auth user ID — used for matching against SenderId / ReceiverId / etc.
        [JsonPropertyName("userId")]
        public Guid UserId { get; set; }

        // User service returns "fullName", not "name"
        [JsonPropertyName("fullName")]
        public string Name { get; set; } = string.Empty;

        // User service returns "username" (lowercase), not "userName"
        [JsonPropertyName("username")]
        public string UserName { get; set; } = string.Empty;

        // User service returns "profilePictureUrl", not "avatarUrl"
        [JsonPropertyName("profilePictureUrl")]
        public string? AvatarUrl { get; set; }
    }

    // ─── Friendship ────────────────────────────────────────────────────────────

    public class FriendshipDto
    {
        public Guid Id { get; set; }
        public UserProfileDto Friend { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    // ─── FriendRequest ─────────────────────────────────────────────────────────

    public class FriendRequestDto
    {
        public Guid Id { get; set; }
        public UserProfileDto Sender { get; set; } = null!;
        public UserProfileDto Receiver { get; set; } = null!;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SendFriendRequestDto
    {
        public Guid ReceiverId { get; set; }
    }

    // ─── Follow ────────────────────────────────────────────────────────────────

    public class FollowDto
    {
        public Guid Id { get; set; }
        public UserProfileDto User { get; set; } = null!;  // context-dependent (follower or followee)
        public DateTime CreatedAt { get; set; }
    }

    // ─── Block ─────────────────────────────────────────────────────────────────

    public class BlockDto
    {
        public Guid Id { get; set; }
        public UserProfileDto BlockedUser { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    // ─── Friend suggestion ─────────────────────────────────────────────────────

    public class FriendSuggestionDto
    {
        public UserProfileDto User { get; set; } = null!;
        public int MutualFriendsCount { get; set; }
    }

    // ─── Social summary ────────────────────────────────────────────────────────

    public class UserSocialSummaryDto
    {
        public Guid UserId { get; set; }
        public int FriendsCount { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }

        // Relationship of the *current* viewer toward this user
        public bool IsFriend { get; set; }
        public bool IsFollowing { get; set; }
        public bool IsBlocked { get; set; }
        public bool HasPendingRequest { get; set; }
    }
}