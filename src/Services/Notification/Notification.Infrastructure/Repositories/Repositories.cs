using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Interfaces;
using Notification.Infrastructure.Data;

namespace Notification.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationDbContext _context;

        public NotificationRepository(NotificationDbContext context) =>
            _context = context;

        public async Task<Domain.Entities.Notification?> GetByIdAsync(Guid id) =>
            await _context.Notifications.FindAsync(id);

        public async Task<IEnumerable<Domain.Entities.Notification>> GetByRecipientAsync(
            Guid recipientId, int page, int pageSize) =>
            await _context.Notifications
                .Where(n => n.RecipientId == recipientId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public async Task<int> GetUnreadCountAsync(Guid recipientId) =>
            await _context.Notifications
                .CountAsync(n => n.RecipientId == recipientId &&
                                 n.Status == NotificationStatus.Unread);

        public async Task AddAsync(Domain.Entities.Notification notification) =>
            await _context.Notifications.AddAsync(notification);

        public async Task MarkAllAsReadAsync(Guid recipientId)
        {
            var unread = await _context.Notifications
                .Where(n => n.RecipientId == recipientId &&
                            n.Status == NotificationStatus.Unread)
                .ToListAsync();

            foreach (var n in unread)
                n.MarkAsRead();
        }
    }

    public class DeviceTokenRepository : IDeviceTokenRepository
    {
        private readonly NotificationDbContext _context;

        public DeviceTokenRepository(NotificationDbContext context) =>
            _context = context;

        public async Task<IEnumerable<DeviceToken>> GetByUserIdAsync(Guid userId) =>
            await _context.DeviceTokens
                .Where(d => d.UserId == userId)
                .ToListAsync();

        public async Task<DeviceToken?> GetByUserAndTokenAsync(Guid userId, string token) =>
            await _context.DeviceTokens
                .FirstOrDefaultAsync(d => d.UserId == userId && d.Token == token);

        public async Task AddAsync(DeviceToken deviceToken) =>
            await _context.DeviceTokens.AddAsync(deviceToken);

        public async Task DeleteAsync(Guid id)
        {
            var token = await _context.DeviceTokens.FindAsync(id);
            if (token != null)
                _context.DeviceTokens.Remove(token);
        }

        public async Task DeleteByTokenAsync(string token)
        {
            var entry = await _context.DeviceTokens
                .FirstOrDefaultAsync(d => d.Token == token);
            if (entry != null)
                _context.DeviceTokens.Remove(entry);
        }
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly NotificationDbContext _context;

        public INotificationRepository Notifications { get; }
        public IDeviceTokenRepository  DeviceTokens  { get; }

        public UnitOfWork(NotificationDbContext context)
        {
            _context      = context;
            Notifications = new NotificationRepository(context);
            DeviceTokens  = new DeviceTokenRepository(context);
        }

        public async Task<int> SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}
