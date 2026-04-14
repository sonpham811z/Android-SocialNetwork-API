using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Post.Application.DTOs;
using Post.Application.Interfaces;

namespace Post.Infrastructure.Services
{
    public class UserProfileHttpClient : IUserProfileHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _userServiceBaseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public UserProfileHttpClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _userServiceBaseUrl = configuration["Services:UserService:BaseUrl"] ?? "http://localhost:5210";
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
    }
}