using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Identity.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services
{
    public class UserServiceClient : IUserServiceClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<UserServiceClient> _logger;

        public UserServiceClient(HttpClient http, ILogger<UserServiceClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task EnsureProfileCreatedAsync(
            Guid userId,
            string email,
            string firstName,
            string lastName,
            DateTime? dateOfBirth,
            string? gender)
        {
            try
            {
                var username = GenerateUsername(email);

                var payload = new
                {
                    userId,
                    email,
                    firstName,
                    lastName,
                    username,
                    dateOfBirth,
                    gender
                };

                var response = await _http.PostAsJsonAsync("api/userprofile", payload);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("UserService profile creation returned {Status}: {Body}", response.StatusCode, body);
                }
            }
            catch (Exception ex)
            {
                // Fire-and-forget: không block registration nếu User service down
                _logger.LogError(ex, "Failed to create profile in User service for userId {UserId}", userId);
            }
        }

        private static string GenerateUsername(string email)
        {
            var local = email.Split('@')[0].ToLower();
            // Chỉ giữ chữ, số, dấu gạch dưới
            local = Regex.Replace(local, @"[^a-z0-9_]", "");
            if (local.Length < 3) local = local.PadRight(3, '0');
            return local;
        }
    }
}
