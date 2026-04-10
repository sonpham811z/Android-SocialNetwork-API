using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Friend.Application.DTOs;
using Friend.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Friend.Infrastructure.Services
{
    // ─── UserProfile HTTP client (Đã refactor gọn gàng) ───────────────────────

    public class UserProfileHttpClient : IUserProfileHttpClient
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly ILogger<UserProfileHttpClient> _logger;

        // Inject thêm ILogger vào đây
        public UserProfileHttpClient(HttpClient http, IConfiguration config, ILogger<UserProfileHttpClient> logger)
        {
            _http = http;
            _logger = logger;
            
            // Ưu tiên lấy từ config, không có thì fallback về port 5000 của bro
            _baseUrl = config["Services:UserService:BaseUrl"] ?? "http://localhost:5000";
            _baseUrl = _baseUrl.TrimEnd('/');
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
        {
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/api/userprofile/{userId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Profile endpoint returned status {StatusCode} for userId {UserId}", response.StatusCode, userId);
                    return null;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>();
                
                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    return apiResponse.Data;
                }
                
                _logger.LogWarning("API returned error for userId {UserId}: {Message}", userId, apiResponse?.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile {UserId} from {_baseUrl}", userId, _baseUrl);
            }

            return null;
        }

        public async Task<List<UserProfileDto>> GetUserProfilesAsync(List<Guid> userIds)
        {
            if (userIds == null || !userIds.Any())
                return new List<UserProfileDto>();

            try
            {
                var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/userprofile/batch", userIds);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Batch endpoint returned status {StatusCode}", response.StatusCode);
                    return new List<UserProfileDto>();
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserProfileDto>>>();
                
                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    _logger.LogInformation("Successfully fetched {Count} profiles from User Service", apiResponse.Data.Count);
                    return apiResponse.Data;
                }
                
                _logger.LogWarning("API returned error: {Message}", apiResponse?.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch fetching profiles from {_baseUrl}", _baseUrl);
            }

            return new List<UserProfileDto>();
        }
    }
}