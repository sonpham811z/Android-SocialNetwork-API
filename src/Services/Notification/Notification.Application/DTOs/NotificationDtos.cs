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
