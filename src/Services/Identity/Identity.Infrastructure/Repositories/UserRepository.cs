using System;
using System.Linq;
using System.Threading.Tasks;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IdentityDbContext _context;

        public UserRepository(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetByGoogleIdAsync(string googleId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var user = await GetByIdAsync(id);

            if (user == null)
                return false;

            user.IsActive = false;
            await UpdateAsync(user);

            return true;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
    }

    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IdentityDbContext _context;
        public RefreshTokenRepository(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User) // include = join in db query
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task RevokeAsync(string token, string revokedByIp)
        {
            var refreshToken = await GetByTokenAsync(token);
            if (refreshToken == null || refreshToken.IsRevoked)
            {
                return;
            }

            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = revokedByIp;
            _context.RefreshTokens.Update(refreshToken);
            
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        // Sau này update thêm revoked all token except current token
        public async Task RevokeAllUserTokensAsync(Guid userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveExpiredTokensAsync(Guid userId)
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && (rt.ExpiresAt < DateTime.UtcNow || rt.RevokedAt != null))
                .ToListAsync();

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
        }
    }

    public class EmailVerificationRepository : IEmailVerificationRepository
    {
        private readonly IdentityDbContext _context;

        public EmailVerificationRepository(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task<EmailVerificationToken> CreateAsync(EmailVerificationToken token)
        {
            _context.EmailVerificationTokens.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<EmailVerificationToken> GetByTokenAsync(string token)
        {
            return await _context.EmailVerificationTokens
                .Include(evt => evt.User)
                .FirstOrDefaultAsync(evt => evt.Token == token);
        }

        public async Task MarkAsUsedAsync(Guid id)
        {
            var token = await _context.EmailVerificationTokens.FindAsync(id);
            if (token != null)
            {
                token.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }
    }
    public class PasswordResetRepository : IPasswordResetRepository
    {
        private readonly IdentityDbContext _context;

        public PasswordResetRepository(IdentityDbContext context)
        {
            _context = context;
        }

        public async Task<PasswordResetToken> CreateAsync(PasswordResetToken token)
        {
            _context.PasswordResetTokens.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<PasswordResetToken> GetByTokenAsync(string token)
        {
            return await _context.PasswordResetTokens
                .Include(prt => prt.User)
                .FirstOrDefaultAsync(prt => prt.Token == token);
        }

        public async Task MarkAsUsedAsync(Guid id)
        {
            var token = await _context.PasswordResetTokens.FindAsync(id);
            if (token != null)
            {
                token.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}