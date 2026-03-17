using Microsoft.EntityFrameworkCore;
using User.Domain.Entities;

namespace User.Infrastructure.Data
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("User");

            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.ToTable("user_profiles");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.UserName);
                entity.HasIndex(e => e.Email);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(256);

                    entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasMaxLength(30);
                
                entity.Property(e => e.Bio)
                    .HasMaxLength(500);
                
                entity.Property(e => e.Location)
                    .HasMaxLength(100);
                
                entity.Property(e => e.City)
                    .HasMaxLength(100);
                
                entity.Property(e => e.Country)
                    .HasMaxLength(100);
                
                entity.Property(e => e.Website)
                    .HasMaxLength(200);
                
                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(20);
                
                entity.Property(e => e.ProfilePictureUrl)
                    .HasMaxLength(500);
                
                entity.Property(e => e.ProfilePicturePublicId)
                    .HasMaxLength(200);
                
                entity.Property(e => e.CoverPhotoUrl)
                    .HasMaxLength(500);
                
                entity.Property(e => e.CoverPhotoPublicId)
                    .HasMaxLength(200);
                
                entity.Property(e => e.Gender)
                    .HasMaxLength(20);
                
                // Relationships
                entity.HasOne(e => e.Settings)
                    .WithOne(s => s.UserProfile)
                    .HasForeignKey<UserSettings>(s => s.UserProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasMany(e => e.Activities)
                    .WithOne(a => a.UserProfile)
                    .HasForeignKey(a => a.UserProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserSettings>(entity =>
            {
                entity.ToTable("user_settings");
                
                entity.HasKey(e => e.Id);
                
                entity.HasIndex(e => e.UserProfileId).IsUnique();
                
                entity.Property(e => e.Language)
                    .HasMaxLength(10)
                    .HasDefaultValue("en");
                
                entity.Property(e => e.Theme)
                    .HasMaxLength(20)
                    .HasDefaultValue("Light");
                
                entity.Property(e => e.PrivacySettings)
                    .HasColumnType("jsonb"); // PostgreSQL JSON type
                
                entity.Property(e => e.NotificationSettings)
                    .HasColumnType("jsonb");
            });

            // UserActivity Configuration
            modelBuilder.Entity<UserActivity>(entity =>
            {
                entity.ToTable("user_activities");
                
                entity.HasKey(e => e.Id);
                
                entity.HasIndex(e => e.UserProfileId);
                entity.HasIndex(e => e.Timestamp);
                
                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasConversion<string>(); // Store enum as string
                
                entity.Property(e => e.Description)
                    .HasMaxLength(500);
                
                entity.Property(e => e.IpAddress)
                    .HasMaxLength(50);
                
                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);
            });
        }
    }
}