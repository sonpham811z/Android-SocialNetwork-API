using System.Threading.Tasks;
using Google.Apis.Auth;
using Identity.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Identity.Infrastructure.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {   
        private readonly IConfiguration _configuration; // dùng đẻ đọc data từ appsetting

        public GoogleAuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<GoogleUserInfo> ValidateGoogleTokenAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] {_configuration["Google:ClientId"]}
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                return new GoogleUserInfo
                {
                    GoogleId = payload.Subject,
                    Email = payload.Email,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                };
            }
            catch
            {
                return null;
            }
        }

    }    
}