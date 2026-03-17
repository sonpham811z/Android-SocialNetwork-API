using System;

namespace Identity.Domain.Events
{
    public abstract class UserEvent
    {
        public Guid UserId { get; set; }
        public DateTime OccuredAt { get; set; }

        protected UserEvent()
        {
            OccuredAt = DateTime.UtcNow;
        }
    }

    public class UserRegisteredEvent : UserEvent
    {
        public string? FirstName { get;set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public string?Email { get; set; }
        public DateTime DateOfBirth { get; set; }
    }

    public class UserDeletedEvent : UserEvent
    {
        public string? Reason { get; set; }
    }

    // Event khi user đăng nhập bằng Google lần đầu
    public class UserGoogleRegisteredEvent : UserEvent
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}