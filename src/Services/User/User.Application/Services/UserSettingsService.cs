using System;
using System.Text.Json;
using System.Threading.Tasks;
using User.Application.DTOs;
using User.Application.Interfaces;
using User.Domain.Entities;
using User.Domain.Interfaces;

namespace User.Application.Services
{
    public class UserSettingsService : IUserSettingsService
    {
        private readonly IUserProfileRepository _profileRepository;
        private readonly IUserSettingsRepository _settingsRepository;
        private readonly IUserActivityRepository _activityRepository;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public UserSettingsService(
            IUserProfileRepository profileRepository,
            IUserSettingsRepository settingsRepository,
            IUserActivityRepository activityRepository)
        {
            _profileRepository = profileRepository;
            _settingsRepository = settingsRepository;
            _activityRepository = activityRepository;
        }

        public async Task<ApiResponse<UserSettingsDto>> GetSettingsAsync(Guid userId)
        {
            var profile = await _profileRepository.GetByUserIdAsync(userId);
            if (profile == null || profile.IsDeleted)
                return ApiResponse<UserSettingsDto>.ErrorResponse("Profile not found");

            var settings = await _settingsRepository.GetByUserProfileIdAsync(profile.Id);
            if (settings == null)
            {
                settings = new UserSettings { UserProfileId = profile.Id };
                settings = await _settingsRepository.CreateAsync(settings);
            }

            return ApiResponse<UserSettingsDto>.SuccessResponse(MapToDto(settings));
        }

        public async Task<ApiResponse<UserSettingsDto>> UpdateSettingsAsync(Guid userId, UpdateSettingsDto dto)
        {
            var profile = await _profileRepository.GetByUserIdAsync(userId);
            if (profile == null || profile.IsDeleted)
                return ApiResponse<UserSettingsDto>.ErrorResponse("Profile not found");

            var settings = await _settingsRepository.GetByUserProfileIdAsync(profile.Id);
            if (settings == null)
            {
                settings = new UserSettings { UserProfileId = profile.Id };
                settings = await _settingsRepository.CreateAsync(settings);
            }

            if (dto.Language != null)
                settings.Language = dto.Language;

            if (dto.Theme != null)
                settings.Theme = dto.Theme;

            if (dto.PrivacySettings != null)
                settings.PrivacySettings = JsonSerializer.Serialize(dto.PrivacySettings);

            if (dto.NotificationSettings != null)
                settings.NotificationSettings = JsonSerializer.Serialize(dto.NotificationSettings);

            settings.UpdatedAt = DateTime.UtcNow;
            await _settingsRepository.UpdateAsync(settings);

            await _activityRepository.CreateAsync(new UserActivity
            {
                UserProfileId = profile.Id,
                Type = ActivityType.SettingsUpdated,
                Description = "Settings updated",
                IpAddress = "0.0.0.0",
                UserAgent = "System"
            });

            return ApiResponse<UserSettingsDto>.SuccessResponse(MapToDto(settings), "Settings updated successfully");
        }

        private UserSettingsDto MapToDto(UserSettings settings)
        {
            PrivacySettingsDto privacyDto = null;
            if (!string.IsNullOrEmpty(settings.PrivacySettings))
            {
                try { privacyDto = JsonSerializer.Deserialize<PrivacySettingsDto>(settings.PrivacySettings, _jsonOptions); }
                catch { }
            }
            privacyDto ??= new PrivacySettingsDto();

            NotificationSettingsDto notifDto = null;
            if (!string.IsNullOrEmpty(settings.NotificationSettings))
            {
                try { notifDto = JsonSerializer.Deserialize<NotificationSettingsDto>(settings.NotificationSettings, _jsonOptions); }
                catch { }
            }
            notifDto ??= new NotificationSettingsDto();

            return new UserSettingsDto
            {
                Language = settings.Language,
                Theme = settings.Theme,
                PrivacySettings = privacyDto,
                NotificationSettings = notifDto
            };
        }
    }
}
