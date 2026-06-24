using System.Threading.Tasks;
using Identity.Application.DTOs;
using Identity.Domain.Entities;

namespace Identity.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<UserDto>> RegisterAsync(RegisterDto dto, string ipAddress);
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto, string ipAddress);
        Task<ApiResponse<AuthResponseDto>> GoogleAuthAsync(GoogleAuthDto dto, string ipAddress);
        Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(string token, string ipAddress);
        Task<ApiResponse<bool>> VerifyEmailAsync(string token);
        Task<ApiResponse<bool>> ResendVerificationEmailAsync(ResendVerificationDto dto);
        Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordDto dto);
        Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task<ApiResponse<bool>> LogoutAsync(string token, string ipAddress);
    }

    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string ValidateToken(string token);
    }

    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }


    public class GoogleUserInfo
    {
        public string GoogleId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo> ValidateGoogleTokenAsync(string idToken);
    }

    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string token);
        Task SendPasswordResetEmailAsync(string email, string token);
    }
}   
