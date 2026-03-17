// Định nghĩa các interface để theo tác ở Infrastructure (Domain ko implement)
using System;
using System.Threading.Tasks; // task ~ promise trong js
using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces
{
    public interface IUserRepository // repository dùng để lưu trữ, truy xuất db
    {
        Task<User> GetByIdAsync(Guid id);
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByGoogleIdAsync(string googleId);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> EmailExistsAsync(string email);
    }
    
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> GetByTokenAsync(string token);
        Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
        Task RevokeAsync(string token, string revokedByIp);
        Task RevokeAllUserTokensAsync(Guid userId);
        Task RemoveExpiredTokensAsync(Guid userId);
    }

    public interface IEmailVerificationRepository
    {
        Task<EmailVerificationToken> CreateAsync(EmailVerificationToken token);
        Task<EmailVerificationToken> GetByTokenAsync(string token);
        Task MarkAsUsedAsync(Guid id);
    }

    public interface IPasswordResetRepository
    {
        Task<PasswordResetToken> CreateAsync(PasswordResetToken token);
        Task<PasswordResetToken> GetByTokenAsync(string token);
        Task MarkAsUsedAsync(Guid id);
    }
}