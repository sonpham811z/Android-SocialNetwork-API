using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace User.Application.DTOs
{
    // Create Profile DTO
    public class CreateUserProfileDto
    {
        [Required]
        public Guid UserId { get; set; } // From Identity Service
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string FirstName { get; set; }
        
        [Required]
        [MinLength(2)]
        [MaxLength(50)]
        public string LastName { get; set; }
        
        [Required]
        [MinLength(3)]
        [MaxLength(30)]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers and underscores")]
        public string Username { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
    }

    // Update Profile DTO
    public class UpdateUserProfileDto
    {
        [MinLength(2)]
        [MaxLength(50)]
        public string? FirstName { get; set; }
        
        [MinLength(2)]
        [MaxLength(50)]
        public string? LastName { get; set; }
        
        [MaxLength(500)]
        public string? Bio { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        public bool? ClearDateOfBirth { get; set; }
        public string? Gender { get; set; }
        
        [MaxLength(100)]
        public string? Location { get; set; }
        
        [MaxLength(100)]
        public string? City { get; set; }
        
        [MaxLength(100)]
        public string? Country { get; set; }
        
        [Url]
        public string? Website { get; set; }
        
        [Phone]
        public string? PhoneNumber { get; set; }
        
        public bool? IsPrivate { get; set; }
    }

    // Upload Image DTO
    public class UploadImageDto
    {
        [Required]
        public IFormFile Image { get; set; }
    }

    // Update Settings DTO
    public class UpdateSettingsDto
    {
        public string? Language { get; set; }
        public string? Theme { get; set; }
        public PrivacySettingsDto? PrivacySettings { get; set; }
        public NotificationSettingsDto? NotificationSettings { get; set; }
    }

    public class PrivacySettingsDto
    {
        public string ProfileVisibility { get; set; } = "public"; // public, friends, onlyMe
        public string WhoCanSeeEmail { get; set; } = "friends";
        public string WhoCanSeeFriends { get; set; } = "friends";
        public string WhoCanSendFriendRequest { get; set; } = "everyone";
    }

    public class NotificationSettingsDto
    {
        public bool PushNotifications { get; set; } = true;
        public bool EmailNotifications { get; set; } = false;
        public bool SmsNotifications { get; set; } = false;
        public bool Likes { get; set; } = true;
        public bool Comments { get; set; } = true;
        public bool Mentions { get; set; } = true;
        public bool NewFollowers { get; set; } = false;
        public bool FriendRequests { get; set; } = true;
        public bool MessageRequests { get; set; } = true;
        public bool DirectMessages { get; set; } = true;
    }

    // Response DTOs
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Username { get; set; }
        public string? Bio { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? Location { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Website { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? CoverPhotoUrl { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsVerified { get; set; }
        public int FriendsCount { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public int PostsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
    }

    public class UserSettingsDto
    {
        public string Language { get; set; }
        public string Theme { get; set; }
        public PrivacySettingsDto PrivacySettings { get; set; }
        public NotificationSettingsDto NotificationSettings { get; set; }
    }

    public class UserActivityDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SearchResultDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public bool IsVerified { get; set; }
        public string Location { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public string[] Errors { get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResponse(string message, string[] errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }

    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
    }
}