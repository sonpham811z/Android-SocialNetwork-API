namespace Notification.Domain.Entities
{
    public class DeviceToken
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }

        /// <summary>FCM registration token from the client app.</summary>
        public string Token { get; private set; } = string.Empty;

        /// <summary>"android" | "ios" | "web"</summary>
        public string Platform { get; private set; } = string.Empty;

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private DeviceToken() { }

        public static DeviceToken Create(Guid userId, string token, string platform)
        {
            return new DeviceToken
            {
                Id        = Guid.NewGuid(),
                UserId    = userId,
                Token     = token,
                Platform  = platform,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public void UpdateToken(string token)
        {
            Token     = token;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
