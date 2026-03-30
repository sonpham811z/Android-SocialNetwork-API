using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task<NotificationEntity?> GetByIdAsync(Guid id);
        Task<IEnumerable<NotificationEntity>> GetByRecipientAsync(Guid recipientId, int page, int pageSize);
        Task<int> GetUnreadCountAsync(Guid recipientId);
        Task AddAsync(NotificationEntity notification);
        Task MarkAllAsReadAsync(Guid recipientId);
    }

    public interface IDeviceTokenRepository
    {
        Task<IEnumerable<Entities.DeviceToken>> GetByUserIdAsync(Guid userId);
        Task<Entities.DeviceToken?> GetByUserAndTokenAsync(Guid userId, string token);
        Task AddAsync(Entities.DeviceToken deviceToken);
        Task DeleteAsync(Guid id);
        Task DeleteByTokenAsync(string token);
    }

    public interface IUnitOfWork : IDisposable
    {
        INotificationRepository Notifications { get; }
        IDeviceTokenRepository  DeviceTokens  { get; }
        Task<int> SaveChangesAsync();
    }
}
