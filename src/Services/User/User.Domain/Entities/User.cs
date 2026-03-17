using System;
using System.Collections.Generic;

namespace User.Domain.Entities
{
    public class UserProfile
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; } //Reference to Identity
        public string Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? Bio { get; set; }
        public string? Gender { get; set; }
        public string? Location { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Website { get; set; }
        public string? PhoneNumber { get; set; }
        
        // Profile Images - Cloudinary URLs
        public string? ProfilePictureUrl { get; set; }
        public string? ProfilePicturePublicId { get; set; } 
        public string? CoverPhotoUrl { get; set; }
        public string? CoverPhotoPublicId { get; set; }
        
        // Privacy & Setting
        public bool IsPrivate { get; set; }
        public bool IsVerified { get; set; }
        
        // Stats
        public int FriendsCount { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public int PostsCount { get; set; }
        
        // Metadata
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public bool IsDeleted { get; set; }
        
        // Navigation Properties
        public UserSettings Settings { get; set; }
        public ICollection<UserActivity> Activities { get; set; }

        public UserProfile()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            IsPrivate = false;
            IsVerified = false;
            IsDeleted = false;
            FriendsCount = 0;
            FollowersCount = 0;
            FollowingCount = 0;
            PostsCount = 0;
            Activities = new List<UserActivity>();
        }

        public string GetFullName() => $"{FirstName} {LastName}";
        public void UpdateLastActivity()
        {
            LastActiveAt = DateTime.UtcNow;
        }
    }

    public class UserSettings
    {
        public Guid Id { get; set; }
        public Guid UserProfileId { get; set; }

        public string Language { get; set; }
        public string Theme { get; set; }

        public string PrivacySettings { get; set; }
        // Example API: 
        // {
        //   "profileVisibility": "friends", // public, friends, onlyMe
        //   "whoCanSeeEmail": "onlyMe",
        //   "whoCanSeeFriends": "friends",
        //   "whoCanSendFriendRequest": "everyone"
        // }

        public string NotificationSettings { get; set; }
        // Example data:
        // {
        //   "emailNotifications": true,
        //   "pushNotifications": true,
        //   "friendRequests": true,
        //   "comments": true,
        //   "likes": true,
        //   "mentions": true
        // }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public UserProfile UserProfile { get; set; }

        public UserSettings() // constructor
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            Language = "en";
            Theme = "Light";

            PrivacySettings = @"{
                ""profileVisibility"": ""friends"",
                ""whoCanSeeEmail"": ""onlyMe"",
                ""whoCanSeeFriends"": ""friends"",
                ""whoCanSendFriendRequest"": ""everyone""
            }";

            NotificationSettings = @"{
                ""emailNotifications"": true,
                ""pushNotifications"": true,
                ""friendRequests"": true,
                ""comments"": true,
                ""likes"": true,
                ""mentions"": true
            }";
        }
    }

    public enum ActivityType
    {
        ProfileCreated,
        ProfileUpdated,
        ProfilePictureChanged,
        CoverPhotoChanged,
        PasswordChanged,
        EmailChanged,
        SettingsUpdated,
        Login,
        Logout
    }

    public class UserActivity
    {
        public Guid Id { get; set; }
        public Guid UserProfileId {get; set; }
        public ActivityType Type { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime Timestamp { get; set; }
        
        // Navigation
        public UserProfile UserProfile { get; set; }
        
        public UserActivity()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }
    }
}