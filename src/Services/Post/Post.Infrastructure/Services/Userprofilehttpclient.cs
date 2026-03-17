using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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

        public UserProfileHttpClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
             _userServiceBaseUrl = configuration["Services:UserService:BaseUrl"] 
                ?? "http://localhost:5002";
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_userServiceBaseUrl}/api/userprofile/{userId}");

                if(!response.IsSuccessStatusCode)
                    return null;
                
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>();

                return apiResponse?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user profile: {ex.Message}");
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
                    var profiles = new List<UserProfileDto>();

                    var response = await _httpClient.PostAsJsonAsync($"{_userServiceBaseUrl}/api/userprofile/batch", userIds);

                    if(!response.IsSuccessStatusCode)
                        return new List<UserProfileDto>();
                    
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserProfileDto>>>();

                    return apiResponse?.Data ?? new List<UserProfileDto>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HttpClient Error] Error in get UserProfiles: {ex.Message}");
                    return new List<UserProfileDto>();
                }
        }

        public async Task<bool> UpdatePostsCountAsync(Guid userId, int count)
        {
            return true;
        }
    }
}