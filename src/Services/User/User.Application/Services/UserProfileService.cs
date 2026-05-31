using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Logging;
using User.Application.DTOs;
using User.Application.Interfaces;
using User.Domain.Entities;
using User.Domain.Interfaces;

namespace User.Application.Services
{
    public class UserProfileService : IUserProfileService
    {
        // private readonly UserDbContext _context;
        private readonly IUserProfileRepository _profileRepository;
        private readonly IUserSettingsRepository _settingsRepository;
        private readonly IUserActivityRepository _activityRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IUnitOfWork _unitOfWork;
    
        public UserProfileService(
            IUserProfileRepository profileRepository,
            IUserSettingsRepository settingsRepository,
            IUserActivityRepository activityRepository,
            ICloudinaryService cloudinaryService,
            IUnitOfWork unitOfWork
        )
        {
            _profileRepository = profileRepository;
            _settingsRepository = settingsRepository;
            _activityRepository = activityRepository;
            _cloudinaryService = cloudinaryService;
            _unitOfWork = unitOfWork;
        }
        private UserProfileDto MapToDto(UserProfile profile)
        {
            return new UserProfileDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Email = profile.Email,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                FullName = profile.GetFullName(),
                Username = profile.UserName,
                Bio = profile.Bio,
                DateOfBirth = profile.DateOfBirth,
                Age = profile.DateOfBirth.HasValue 
                    ? DateTime.Now.Year - profile.DateOfBirth.Value.Year 
                    : null,
                Gender = profile.Gender,
                Location = profile.Location,
                City = profile.City,
                Country = profile.Country,
                Website = profile.Website,
                PhoneNumber = profile.PhoneNumber,
                ProfilePictureUrl = profile.ProfilePictureUrl,
                CoverPhotoUrl = profile.CoverPhotoUrl,
                IsPrivate = profile.IsPrivate,
                IsVerified = profile.IsVerified,
                FriendsCount = profile.FriendsCount,
                FollowersCount = profile.FollowersCount,
                FollowingCount = profile.FollowingCount,
                PostsCount = profile.PostsCount,
                CreatedAt = profile.CreatedAt,
                LastActiveAt = profile.LastActiveAt
            };
        }

        private Task LogSystemActivityAsync(Guid userProfileId, ActivityType type, string description)
        {
            return _activityRepository.CreateAsync(new UserActivity
            {
                UserProfileId = userProfileId,
                Type = type,
                Description = description,
                IpAddress = "0.0.0.0",
                UserAgent = "System"
            });
        }

        public async Task CreateProfileFromIdentityAsync(
            Guid userId, 
            string email, 
            string firstName, 
            string lastName, 
            DateTime dateOfBirth, 
            string? gender
            )
        {

            try
            {
                // Check xem profile đã tồn tại chưa (tránh duplicate)
                var existingProfile = await _profileRepository.GetByUserIdAsync(userId);
                if (existingProfile != null)
                {
                    Console.WriteLine("Profile is already exists");
                    return;
                }


                // Tạo username từ email
                var username = email.Split('@')[0].ToLower();
                
                // Check username trùng, nếu trùng thêm số random
                var usernameExists = await _profileRepository.UsernameExistsAsync(username);
                if (usernameExists)
                {
                    username = $"{username}{new Random().Next(1000, 9999)}";
                }

                // Create profile
                var profile = new UserProfile
                {
                    UserId = userId,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    UserName = username,
                    DateOfBirth = dateOfBirth,
                    Gender = gender,
                };

                profile = await _profileRepository.CreateAsync(profile);

                // Create default settings
                var settings = new UserSettings
                {
                    UserProfileId = profile.Id
                };
                await _settingsRepository.CreateAsync(settings);

                // Log activity
                await LogSystemActivityAsync(
                    profile.Id,
                    ActivityType.ProfileCreated,
                    "Profile created from Identity service"
                );

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating profile from Identity: {ex.Message}");
                throw;
            }
        }

        public async Task<ApiResponse<List<UserProfileDto>>> GetProfilesBatchAsync(List<Guid> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return ApiResponse<List<UserProfileDto>>.SuccessResponse(new List<UserProfileDto>());
            }

            var uniqueIds = userIds.Distinct().ToList();

            var profiles = await _profileRepository.GetByUserIdsAsync(uniqueIds);

            var dtos = profiles.Select(MapToDto).ToList();

            return ApiResponse<List<UserProfileDto>>.SuccessResponse(dtos);
        }
        public async Task UpdateProfileFromIdentityAsync(
            Guid userId, 
            string? firstName, 
            string? lastName, 
            string? gender)
        {
            try
            {
                var profile = await _profileRepository.GetByUserIdAsync(userId);
                if (profile == null || profile.IsDeleted) return;

                bool hasChanges = false;

                if (!string.IsNullOrEmpty(firstName) && profile.FirstName != firstName)
                {
                    profile.FirstName = firstName;
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(lastName) && profile.LastName != lastName)
                {
                    profile.LastName = lastName;
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(gender) && profile.Gender != gender)
                {
                    profile.Gender = gender;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    profile.UpdatedAt = DateTime.UtcNow;
                    await _profileRepository.UpdateAsync(profile);

                    // Log activity
                    await LogSystemActivityAsync(
                        profile.Id,
                        ActivityType.ProfileUpdated,
                        "Profile updated from Identity service"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile from Identity: {ex.Message}");
            }
        }

        public async Task SoftDeleteProfileAsync(Guid userId, string reason)
        {
            try
            {
                var profile = await _profileRepository.GetByUserIdAsync(userId);
                if (profile == null) return;

                profile.IsDeleted = true;
                profile.UpdatedAt = DateTime.UtcNow;
                await _profileRepository.UpdateAsync(profile);

                // Log activity
                await LogSystemActivityAsync(
                    profile.Id,
                    ActivityType.ProfileUpdated,
                    $"Profile deleted from Identity service. Reason: {reason}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting profile from Identity: {ex.Message}");
            }
        }
        public async Task<ApiResponse<UserProfileDto>> GetProfileByIdAsync(Guid id)
        {
            var profile = await _profileRepository.GetByIdAsync(id);

            if(profile == null || profile.IsDeleted)
                return ApiResponse<UserProfileDto>.ErrorResponse("Profile not found");


            return ApiResponse<UserProfileDto>.SuccessResponse(MapToDto(profile));
        }

        public async Task<ApiResponse<UserProfileDto>> GetProfileByUsernameAsync(string userName)
        {
            var profile = await _profileRepository.GetByUsernameAsync(userName);
            if(profile == null || profile.IsDeleted)
                return ApiResponse<UserProfileDto>.ErrorResponse("Profile not found");

            return ApiResponse<UserProfileDto>.SuccessResponse(MapToDto(profile));

        }

        public async Task<ApiResponse<UserProfileDto>> GetProfileByUserIdAsync(Guid userId)
        {
            var profile = await _profileRepository.GetByUserIdAsync(userId);
            
            if (profile == null || profile.IsDeleted)
            {
                return ApiResponse<UserProfileDto>.ErrorResponse("Profile not found");
            }

            return ApiResponse<UserProfileDto>.SuccessResponse(MapToDto(profile));
        }

        public async Task<ApiResponse<UserProfileDto>> CreateProfileAsync(CreateUserProfileDto dto)
        {
            if(await _profileRepository.UsernameExistsAsync(dto.Username))
                return ApiResponse<UserProfileDto>.ErrorResponse("Username already taken");

            if (await _profileRepository.EmailExistsAsync(dto.Email))
                return ApiResponse<UserProfileDto>.ErrorResponse("Email already registered");

            var profile = new UserProfile
            {
                UserId = dto.UserId,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserName = dto.Username.ToLower(),
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender
            };

            profile = await _profileRepository.CreateAsync(profile);

            //Create default settings
            var settings = new UserSettings
            {
                UserProfileId = profile.Id
            };
            await _settingsRepository.CreateAsync(settings);

            await LogSystemActivityAsync(
                profile.Id,
                ActivityType.ProfileCreated,
                "Profile created"
            );

            return ApiResponse<UserProfileDto>.SuccessResponse(
                MapToDto(profile),
                "Profile created successfuly"
            );
            
        }

        public async Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateUserProfileDto dto)
        {
            var profile = await _profileRepository.GetByUserIdAsync(userId);
            
            if (profile == null || profile.IsDeleted)
            {
                return ApiResponse<UserProfileDto>.ErrorResponse("Profile not found");
            }

            Console.WriteLine($"Updating profile {profile.Id} with data: {System.Text.Json.JsonSerializer.Serialize(dto)}");

            if (dto.FirstName != null)
            {
                if (string.IsNullOrWhiteSpace(dto.FirstName))
                {
                    return ApiResponse<UserProfileDto>.ErrorResponse("First name cannot be empty");
                }
                profile.FirstName = dto.FirstName.Trim();
            }

            if (dto.LastName != null)
            {
                if (string.IsNullOrWhiteSpace(dto.LastName))
                {
                    return ApiResponse<UserProfileDto>.ErrorResponse("Last name cannot be empty");
                }
                profile.LastName = dto.LastName.Trim();
            }

            if (dto.Bio != null)
                profile.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();
            
            if (dto.DateOfBirth.HasValue)
                profile.DateOfBirth = dto.DateOfBirth;
            else if (dto.ClearDateOfBirth == true)
                profile.DateOfBirth = null;
            
            if (dto.Gender != null)
                profile.Gender = string.IsNullOrWhiteSpace(dto.Gender) ? null : dto.Gender.Trim();
            
            if (dto.Location != null)
                profile.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
            
            if (dto.City != null)
                profile.City = string.IsNullOrWhiteSpace(dto.City) ? null : dto.City.Trim();
            
            if (dto.Country != null)
                profile.Country = string.IsNullOrWhiteSpace(dto.Country) ? null : dto.Country.Trim();
            
            if (dto.Website != null)
                profile.Website = string.IsNullOrWhiteSpace(dto.Website) ? null : dto.Website.Trim();
            
            if (dto.PhoneNumber != null)
                profile.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
            
            if (dto.IsPrivate.HasValue)
                profile.IsPrivate = dto.IsPrivate.Value;

            profile.UpdatedAt = DateTime.UtcNow;
            
            profile = await _profileRepository.UpdateAsync(profile);

            // Log activity
            await LogSystemActivityAsync(
                profile.Id,
                ActivityType.ProfileUpdated,
                "Profile updated"
            );

            return ApiResponse<UserProfileDto>.SuccessResponse(
                MapToDto(profile), 
                "Profile updated successfully"
            );
        }

        public async Task<ApiResponse<bool>> DeleteProfileAsync(Guid userId)
        {
            var profile = await _profileRepository.GetByUserIdAsync(userId);
            
            if (profile == null)
            {
                return ApiResponse<bool>.ErrorResponse("Profile not found");
            }

            var result = await _profileRepository.DeleteAsync(profile.Id);
            
            if (!result)
            {
                return ApiResponse<bool>.ErrorResponse("Failed to delete profile");
            }

            return ApiResponse<bool>.SuccessResponse(true, "Profile deleted successfully");
        }

        public async Task<ApiResponse<PaginatedResponse<SearchResultDto>>> SearchUsersAsync(
            string searchTerm, 
            int pageNumber, 
            int pageSize)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return ApiResponse<PaginatedResponse<SearchResultDto>>.ErrorResponse("Search term is required");
            }

            var skip       = (pageNumber - 1) * pageSize;
            var profiles   = await _profileRepository.SearchByNameAsync(searchTerm, skip, pageSize);
            var totalCount = await _profileRepository.CountByNameAsync(searchTerm);

            var results = profiles.Select(p => new SearchResultDto
            {
                Id = p.UserId,   // Identity user ID — dùng để navigate và gọi API
                Username = p.UserName,
                FullName = p.GetFullName(),
                ProfilePictureUrl = p.ProfilePictureUrl,
                IsVerified = p.IsVerified,
                Location = p.Location
            });

            var response = new PaginatedResponse<SearchResultDto>
            {
                Items = results,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ApiResponse<PaginatedResponse<SearchResultDto>>.SuccessResponse(response);
        }

        public async Task<ApiResponse<string>> UploadProfilePictureAsync(Guid userId, IFormFile image)
        {
            var profile = await _profileRepository.GetByUserIdAsync(userId);
            
            if (profile == null || profile.IsDeleted)
            {
                return ApiResponse<string>.ErrorResponse("Profile not found");
            }

            // Delete old image if exists
            if (!string.IsNullOrEmpty(profile.ProfilePicturePublicId))
            {
                await _cloudinaryService.DeleteImageAsync(profile.ProfilePicturePublicId);
            }

            // Upload new image
            var result = await _cloudinaryService.UploadImageAsync(image, "profile_pictures");
            
            if (!result.Success)
            {
                return ApiResponse<string>.ErrorResponse($"Upload failed: {result.Error}");
            }

            // Update profile
            profile.ProfilePictureUrl = result.Url;
            profile.ProfilePicturePublicId = result.PublicId;
            profile.UpdatedAt = DateTime.UtcNow;
            
            await _profileRepository.UpdateAsync(profile);

            // Log activity
            await LogSystemActivityAsync(
                profile.Id,
                ActivityType.ProfilePictureChanged,
                "Profile picture updated"
            );

            return ApiResponse<string>.SuccessResponse(result.Url, "Profile picture uploaded successfully");
        }

        public async Task<ApiResponse<string>> UploadCoverPhotoAsync(Guid userId, IFormFile image)
        {
            var profile = await _profileRepository.GetByUserIdAsync(userId);
            
            if (profile == null || profile.IsDeleted)
            {
                return ApiResponse<string>.ErrorResponse("Profile not found");
            }

            // Delete old image if exists
            if (!string.IsNullOrEmpty(profile.CoverPhotoPublicId))
            {
                await _cloudinaryService.DeleteImageAsync(profile.CoverPhotoPublicId);
            }

            // Upload new image
            var result = await _cloudinaryService.UploadImageAsync(image, "cover_photos");
            
            if (!result.Success)
            {
                return ApiResponse<string>.ErrorResponse($"Upload failed: {result.Error}");
            }

            // Update profile
            profile.CoverPhotoUrl = result.Url;
            profile.CoverPhotoPublicId = result.PublicId;
            profile.UpdatedAt = DateTime.UtcNow;
            
            await _profileRepository.UpdateAsync(profile);

            // Log activity
            await LogSystemActivityAsync(
                profile.Id,
                ActivityType.CoverPhotoChanged,
                "Cover photo updated"
            );

            return ApiResponse<string>.SuccessResponse(result.Url, "Cover photo uploaded successfully");
        }

        public async Task<ApiResponse<bool>> DeleteProfilePictureAsync(Guid userId)
        {
            var profile = await _profileRepository.GetByUserIdAsync(userId);
            
            if (profile == null || profile.IsDeleted)
            {
                return ApiResponse<bool>.ErrorResponse("Profile not found");
            }

            if (!string.IsNullOrEmpty(profile.ProfilePicturePublicId))
            {
                await _cloudinaryService.DeleteImageAsync(profile.ProfilePicturePublicId);
            }

            profile.ProfilePictureUrl = null;
            profile.ProfilePicturePublicId = null;
            profile.UpdatedAt = DateTime.UtcNow;
            
            await _profileRepository.UpdateAsync(profile);

            return ApiResponse<bool>.SuccessResponse(true, "Profile picture deleted successfully");
        }

        public async Task<ApiResponse<bool>> DeleteCoverPhotoAsync(Guid userId)
        {
            var profile = await _profileRepository.GetByUserIdAsync(userId);
            
            if (profile == null || profile.IsDeleted)
            {
                return ApiResponse<bool>.ErrorResponse("Profile not found");
            }

            if (!string.IsNullOrEmpty(profile.CoverPhotoPublicId))
            {
                await _cloudinaryService.DeleteImageAsync(profile.CoverPhotoPublicId);
            }

            profile.CoverPhotoUrl = null;
            profile.CoverPhotoPublicId = null;
            profile.UpdatedAt = DateTime.UtcNow;
            
            await _profileRepository.UpdateAsync(profile);

            return ApiResponse<bool>.SuccessResponse(true, "Cover photo deleted successfully");
        }

    }
}