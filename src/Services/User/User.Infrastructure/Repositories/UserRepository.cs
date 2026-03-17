using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using User.Domain.Entities;
using User.Domain.Interfaces;
using User.Infrastructure.Data;

namespace User.Infrastructure.Repositories
{
    public class UserProfileRepository : IUserProfileRepository
    {
        private readonly UserDbContext _context;

        public UserProfileRepository(UserDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserProfile>> GetByUserIdsAsync(IEnumerable<Guid> userIds)
        {
            return await _context.UserProfiles
                .Where(p => userIds.Contains(p.UserId) && !p.IsDeleted)
                .ToListAsync();
        }

        public async Task<UserProfile> GetByIdAsync(Guid id)
        {
            return await _context.UserProfiles
                .Include(p=>p.Settings)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task<UserProfile> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserProfiles
                .Include(p => p.Settings)
                .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);
        }

        public async Task<UserProfile> GetByUsernameAsync(string userName)
        {
            return await _context.UserProfiles
                .Include(p => p.Settings)
                .FirstOrDefaultAsync(p => p.UserName == userName.ToLower() && !p.IsDeleted);
        }

        public async Task<UserProfile> GetByEmailAsync(string email)
        {
            return await _context.UserProfiles
                .Include(p => p.Settings)
                .FirstOrDefaultAsync(p => p.Email == email.ToLower() && !p.IsDeleted);
        }

        public async Task<IEnumerable<UserProfile>> SearchByNameAsync(string searchTerm, int skip, int take)
        {
            var lowerSearchTerm = searchTerm.ToLower();

            return await _context.UserProfiles
                .Where(p => !p.IsDeleted && 
                           (p.FirstName.ToLower().Contains(lowerSearchTerm) ||
                            p.LastName.ToLower().Contains(lowerSearchTerm) ||
                            p.UserName.ToLower().Contains(lowerSearchTerm)))
                .OrderBy(p => p.FirstName)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<UserProfile> CreateAsync(UserProfile profile)
        {
            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<UserProfile> UpdateAsync(UserProfile profile)
        {
            profile.UpdatedAt = DateTime.UtcNow;
            _context.UserProfiles.Update(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var profile = await GetByIdAsync(id);
            if (profile == null) return false;
            
            profile.IsDeleted = true;
            profile.UpdatedAt = DateTime.UtcNow;
            await UpdateAsync(profile);
            return true;
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _context.UserProfiles
                .AnyAsync(p => p.UserName == username.ToLower() && !p.IsDeleted);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.UserProfiles
                .AnyAsync(p => p.Email == email.ToLower() && !p.IsDeleted);
        }

        public async Task<IEnumerable<UserProfile>> GetActiveUsersAsync(int skip, int take)
        {
            return await _context.UserProfiles
                .Where (p => !p.IsDeleted)
                .OrderByDescending(p => p.LastActiveAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }

    public class UserSettingsRepository : IUserSettingsRepository
    {
        private readonly UserDbContext _context;

        public UserSettingsRepository(UserDbContext context)
        {
            _context = context;
        }

        public async Task<UserSettings> GetByUserProfileIdAsync(Guid userProfileId)
        {
            return await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserProfileId == userProfileId);
        }

        public async Task<UserSettings> CreateAsync(UserSettings settings)
        {
            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync();
            return settings;
        }

        public async Task<UserSettings> UpdateAsync(UserSettings settings)
        {
            settings.UpdatedAt = DateTime.UtcNow;
            _context.UserSettings.Update(settings);
            await _context.SaveChangesAsync();
            return settings;
        }
    }

    public class UserActivityRepository : IUserActivityRepository
    {
        private readonly UserDbContext _context;

        public UserActivityRepository(UserDbContext context)
        {
            _context = context;
        }

        public async Task<UserActivity> CreateAsync(UserActivity activity)
        {
            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();
            return activity;
        }

        public async Task<IEnumerable<UserActivity>> GetUserActivitiesAsync(Guid userProfileId, int skip, int take)
        {
            return await _context.UserActivities
                .Where(a => a.UserProfileId == userProfileId)
                .OrderByDescending(a => a.Timestamp)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task DeleteOldActivitiesAsync(DateTime olderThan)
        {
            var oldActivities = await _context.UserActivities
                .Where(a => a.Timestamp < olderThan)
                .ToListAsync();
            
            _context.UserActivities.RemoveRange(oldActivities);
            await _context.SaveChangesAsync();
        }
    }
}
