using Notification.Domain.Enums;

namespace Notification.Application.DTOs
{
    public class NotificationDto
    {
        public Guid               Id          { get; init; }
        public Guid               RecipientId { get; init; }
        public Guid               ActorId     { get; init; }
        public NotificationType   Type        { get; init; }
        public NotificationStatus Status      { get; init; }
        public string             Message     { get; init; } = string.Empty;
        public Guid?              ReferenceId { get; init; }
        public DateTime           CreatedAt   { get; init; }
        public DateTime?          ReadAt      { get; init; }
    }

    public class RegisterDeviceTokenDto
    {
        public string Token    { get; init; } = string.Empty;
        public string Platform { get; init; } = string.Empty;  // "android" | "ios" | "web"
    }

    /// <summary>
    /// A recipient's notification preferences, fetched from the User service.
    /// Mirrors the toggles on the client's notification-settings screen.
    /// Defaults are permissive so a missing/unreachable settings record never suppresses notifications.
    /// </summary>
    public class NotificationPreferences
    {
        public bool PushNotifications { get; init; } = true;   // master switch for FCM (offline push)
        public bool Likes            { get; init; } = true;
        public bool Comments         { get; init; } = true;
        public bool Mentions         { get; init; } = true;
        public bool NewFollowers     { get; init; } = true;
        public bool FriendRequests   { get; init; } = true;
        public bool MessageRequests  { get; init; } = true;
        public bool DirectMessages   { get; init; } = true;
    }

    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Items       { get; init; } = [];
        public int            TotalCount  { get; init; }
        public int            Page        { get; init; }
        public int            PageSize    { get; init; }
        public int            TotalPages  => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class ApiResponse<T>
    {
        public bool   Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public T?     Data    { get; init; }

        public static ApiResponse<T> Ok(T data, string message = "Success") =>
            new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
