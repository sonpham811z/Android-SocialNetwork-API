using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Post.Application.DTOs
{
    // Common Response
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = new List<string>()
            };
        }

        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = errors ?? new List<string>()
            };
        }
    }

    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; }
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // Post DTOs
    public class PostDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public UserProfileDto User { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
        public string? ImageUrl { get; set; }
        public string? AudioUrl { get; set; }
        public string? AudioDuration { get; set; }
        public List<double>? Waveform { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public int SharesCount { get; set; }
        public string Visibility { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public List<CommentDto>? Comments { get; set; }

        // Share reference
        public Guid? OriginalPostId { get; set; }
        public PostDto? OriginalPost { get; set; }
    }

    public class SharePostDto
    {
        public string Content { get; set; } = string.Empty;
        public string Visibility { get; set; } = "Public";
    }

    public class CreateTextPostDto
    {
        public string Content { get; set; }
        public string Visibility { get; set; } = "Public";
    }

    public class CreateImagePostDto
    {
        public string Content { get; set; }
        public string Visibility { get; set; } = "Public";
        // Image will be uploaded via IFormFile
    }

    public class CreateVoicePostDto
    {
        public string Content { get; set; }
        public string Visibility { get; set; } = "Public";
        // Audio will be uploaded via IFormFile
    }

    public class UpdatePostDto
    {
        public string Content { get; set; }
        public string? Visibility { get; set; }
    }

    // Comment DTOs
    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid UserId { get; set; }
        public UserProfileDto User { get; set; }
        public string Content { get; set; }
        public Guid? ParentCommentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int LikesCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
    }

    public class CreateCommentDto
    {
        public string Content { get; set; }
        public Guid? ParentCommentId { get; set; }
    }

    public class UpdateCommentDto
    {
        public string Content { get; set; }
    }

    // User Profile DTO (simplified for posts)
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Name { get; set; }
        
        [JsonPropertyName("username")]
        public string? UserName { get; set; }
        
        public string? ProfilePictureUrl { get; set; }
        public bool IsVerified { get; set; }
        
        // Fields from User Service API response - these will be used if Name/UserName are null
        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }
        
        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }
        
        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }
        
        // Helper method to get display name
        public string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(Name))
                return Name;
            
            if (!string.IsNullOrWhiteSpace(FullName))
                return FullName;
            
            if (!string.IsNullOrWhiteSpace(FirstName) || !string.IsNullOrWhiteSpace(LastName))
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(FirstName)) parts.Add(FirstName);
                if (!string.IsNullOrWhiteSpace(LastName)) parts.Add(LastName);
                return string.Join(" ", parts);
            }
            
            return "Unknown User";
        }
        
        public string GetUsername()
        {
            return !string.IsNullOrWhiteSpace(UserName) ? UserName : "unknown";
        }
    }

    // Feed DTOs
    public class FeedRequestDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}