using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Post.Application.DTOs;
using Post.Application.Interfaces;

namespace Post.Infrastructure.Services
{
    public class UserProfileHttpClient : IUserProfileHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _userServiceBaseUrl;
        private readonly string _friendServiceBaseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public UserProfileHttpClient(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _userServiceBaseUrl = configuration["Services:UserService:BaseUrl"] ?? "http://localhost:5210";
            _friendServiceBaseUrl = configuration["Services:FriendService:BaseUrl"] ?? "http://localhost:5176";
            _httpContextAccessor = httpContextAccessor;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
        {
            try
            {
                var url = $"{_userServiceBaseUrl}/api/userprofile/userid/{userId}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[UserProfileHttpClient] GET {url} -> {(int)response.StatusCode} {response.StatusCode}. Body: {errorContent}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserProfileDto>>(content, _jsonOptions);

                return apiResponse?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserProfileHttpClient] Error fetching user profile: {ex.Message}");
                return null;
            }

        }

        public async Task<List<UserProfileDto>> GetUserProfilesAsync(List<Guid> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return new List<UserProfileDto>();
            }

            try
            {
                var url = $"{_userServiceBaseUrl}/api/userprofile/batch";
                var response = await _httpClient.PostAsJsonAsync(url, userIds);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[UserProfileHttpClient] POST {url} -> {(int)response.StatusCode} {response.StatusCode}. Body: {errorContent}");
                    return new List<UserProfileDto>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<UserProfileDto>>>(content, _jsonOptions);

                return apiResponse?.Data ?? new List<UserProfileDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserProfileHttpClient] Error fetching user profiles: {ex.Message}");
                return new List<UserProfileDto>();
            }
        }

        public Task<bool> UpdatePostsCountAsync(Guid userId, int count)
        {
            return Task.FromResult(true);
        }

        public async Task<Guid?> GetUserIdByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            try
            {
                var url = $"{_userServiceBaseUrl}/api/userprofile/username/{Uri.EscapeDataString(username)}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<UserProfileDto>>(content, _jsonOptions);
                var dto = apiResponse?.Data;
                if (dto == null) return null;

                if (dto.UserId != Guid.Empty) return dto.UserId;
                if (dto.Id != Guid.Empty) return dto.Id;
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserProfileHttpClient] Error resolving username '{username}': {ex.Message}");
                return null;
            }
        }

        public async Task<List<Guid>> GetFriendIdsAsync(Guid userId)
        {
            try
            {
                var url = $"{_friendServiceBaseUrl}/api/friends/ids";
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                var bearerToken = _httpContextAccessor.HttpContext?
                    .Request
                    .Headers["Authorization"]
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(bearerToken) && bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    request.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerToken);
                }

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return new List<Guid>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<Guid>>>(content, _jsonOptions);
                return apiResponse?.Data ?? new List<Guid>();
            }
            catch
            {
                return new List<Guid>();
            }
        }
    }
}