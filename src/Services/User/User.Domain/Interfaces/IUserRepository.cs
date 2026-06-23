
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using User.Domain.Entities;

namespace User.Domain.Interfaces
{
    public interface IUserProfileRepository
    {
        Task<UserProfile> GetByIdAsync(Guid id);
        Task<UserProfile> GetByUserIdAsync(Guid userId);
        /// <summary>Lấy hồ sơ theo UserId kể cả khi đã xóa mềm (bỏ qua query filter).</summary>
        Task<UserProfile?> GetByUserIdIncludingDeletedAsync(Guid userId);
        Task<UserProfile> GetByUsernameAsync(string username);
        Task<UserProfile> GetByEmailAsync(string email);
        Task<IEnumerable<UserProfile>> GetByUserIdsAsync(IEnumerable<Guid> userIds);
        Task<IEnumerable<UserProfile>> SearchByNameAsync(string searchTerm, int skip, int take);
        Task<int> CountByNameAsync(string searchTerm);
        Task<UserProfile> CreateAsync(UserProfile profile);
        Task<UserProfile> UpdateAsync(UserProfile profile);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<IEnumerable<UserProfile>> GetActiveUsersAsync(int skip, int take);
    }

    public interface IUserSettingsRepository
    {
        Task<UserSettings> GetByUserProfileIdAsync(Guid userProfileId);
        Task<UserSettings> CreateAsync(UserSettings settings);
        Task<UserSettings> UpdateAsync(UserSettings settings);
    }

    public interface IUserActivityRepository
    {
        Task<UserActivity> CreateAsync(UserActivity activity);
        Task<IEnumerable<UserActivity>> GetUserActivitiesAsync(Guid userProfileId, int skip, int take);
        Task DeleteOldActivitiesAsync(DateTime olderThan);
    }
}



