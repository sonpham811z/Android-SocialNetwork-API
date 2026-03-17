using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Dùng để định nghĩa độ dài nếu cần
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Domain.Entities
{
    // 1. USER ENTITY
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }

        public string? PasswordHash { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }

        public bool IsEmailConfirmed { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? GoogleId { get; set; }
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
        public User()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow; // Luôn dùng UtcNow
            IsActive = true;
            IsEmailConfirmed = false;
            RefreshTokens = new List<RefreshToken>();
        }
    }

    // 2. REFRESH TOKEN
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }

        // Foreign Key
        public Guid UserId { get; set; }
        
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!; // null! để báo compiler là EF sẽ tự điền

        public required string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public required string CreatedByIp { get; set; }
        
        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }

        // Computed Properties (Logic)
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt != null;
        public bool IsActive => !IsRevoked && !IsExpired;
    }

    // 3. EMAIL VERIFICATION TOKEN
    public class EmailVerificationToken
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        public required string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsUsed { get; set; }
    }

    // 4. PASSWORD RESET TOKEN
    public class PasswordResetToken
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        public required string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsUsed { get; set; }
    }
}