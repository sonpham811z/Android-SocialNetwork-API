using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using User.Application.DTOs;

namespace User.Application.Interfaces
{
    public interface IUserProfileService
    {
        Task<ApiResponse<UserProfileDto>> GetProfileByIdAsync(Guid id);
        Task<ApiResponse<UserProfileDto>> GetProfileByUsernameAsync(string username);
        Task<ApiResponse<UserProfileDto>> GetProfileByUserIdAsync(Guid userId);
        Task<ApiResponse<List<UserProfileDto>>> GetProfilesBatchAsync(List<Guid> userIds);
        Task<ApiResponse<UserProfileDto>> CreateProfileAsync(CreateUserProfileDto dto);
        Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateUserProfileDto dto);
        Task<ApiResponse<bool>> DeleteProfileAsync(Guid userId);
        Task<ApiResponse<PaginatedResponse<SearchResultDto>>> SearchUsersAsync(string searchTerm, int pageNumber, int pageSize);
        Task<ApiResponse<string>> UploadProfilePictureAsync(Guid userId, IFormFile image);
        Task<ApiResponse<string>> UploadCoverPhotoAsync(Guid userId, IFormFile image);
        Task<ApiResponse<bool>> DeleteProfilePictureAsync(Guid userId);
        Task<ApiResponse<bool>> DeleteCoverPhotoAsync(Guid userId);

        Task CreateProfileFromIdentityAsync(Guid userId, string email, string firstName, 
            string lastName, DateTime dateOfBirth, string? gender);
        
        Task UpdateProfileFromIdentityAsync(Guid userId, string? firstName, 
            string? lastName, string? gender);
        
        Task SoftDeleteProfileAsync(Guid userId, string reason);
    }

    public interface IUserSettingsService
    {
        Task<ApiResponse<UserSettingsDto>> GetSettingsAsync(Guid userId);
        Task<ApiResponse<UserSettingsDto>> UpdateSettingsAsync(Guid userId, UpdateSettingsDto dto);
    }

    public interface IUserActivityService
    {
        Task LogActivityAsync(Guid userId, string activityType, string description, string ipAddress, string userAgent);
        Task<ApiResponse<IEnumerable<UserActivityDto>>> GetActivitiesAsync(Guid userId, int pageNumber, int pageSize);
    }

    public interface ICloudinaryService
    {
        Task<CloudinaryUploadResult> UploadImageAsync(IFormFile file, string folder);
        Task<bool> DeleteImageAsync(string publicId);
    }

    public class CloudinaryUploadResult
    {
        public bool Success { get; set; }
        public string Url { get; set; }
        public string PublicId { get; set; }
        public string Error { get; set; }
    }
}