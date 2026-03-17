using System;

namespace User.Application.DTOs
{
    // DTO cho event UserRegistered
    public class UserRegisteredEventDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    // DTO cho event UserGoogleRegistered
    public class UserGoogleRegisteredEventDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    // DTO cho event UserProfileUpdated
    public class UserProfileUpdatedEventDto
    {
        public Guid UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Gender { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    // DTO cho event UserDeleted
    public class UserDeletedEventDto
    {
        public Guid UserId { get; set; }
        public string Reason { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}