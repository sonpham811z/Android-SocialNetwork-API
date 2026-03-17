using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Friend.Application.DTOs;
using Friend.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Friend.Infrastructure.Services
{
    // ─── Redis cache ───────────────────────────────────────────────────────────

    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _db;
        private readonly IServer _server;
        private readonly string _instanceName;

        public RedisCacheService(IConnectionMultiplexer redis, IConfiguration config)
        {
            _db = redis.GetDatabase();
            _server = redis.GetServer(redis.GetEndPoints().First());
            _instanceName = config["Redis:InstanceName"] ?? "friend:";
        }

        private string Key(string key) => $"{_instanceName}{key}";

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(Key(key));
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value.ToString());
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(Key(key), json, expiry ?? TimeSpan.FromMinutes(10));
        }

        public async Task RemoveAsync(string key) =>
            await _db.KeyDeleteAsync(Key(key));

        public async Task RemoveByPrefixAsync(string prefix)
        {
            var pattern = $"{_instanceName}{prefix}*";
            var keys = _server.Keys(pattern: pattern).ToArray();
            if (keys.Length > 0)
                await _db.KeyDeleteAsync(keys);
        }
    }

    // ─── UserProfile HTTP client ───────────────────────────────────────────────

    public class UserProfileHttpClient : IUserProfileHttpClient
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        public UserProfileHttpClient(HttpClient http, IConfiguration config)
        {
            _http = http;
            _baseUrl = config["Services:UserService:BaseUrl"] ?? "http://localhost:5002";
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
        {
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/api/userprofile/{userId}");
                if (!response.IsSuccessStatusCode) return null;

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>();
                return apiResponse?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserProfileHttpClient] Error fetching profile {userId}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<UserProfileDto>> GetUserProfilesAsync(List<Guid> userIds)
        {
            if (userIds == null || !userIds.Any())
                return new List<UserProfileDto>();

            try
            {
                var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/userprofile/batch", userIds);
                if (!response.IsSuccessStatusCode) return new List<UserProfileDto>();

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserProfileDto>>>();
                return apiResponse?.Data ?? new List<UserProfileDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserProfileHttpClient] Error batch fetching profiles: {ex.Message}");
                return new List<UserProfileDto>();
            }
        }
    }
}