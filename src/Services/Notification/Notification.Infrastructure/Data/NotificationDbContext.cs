using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
            : base(options) { }

        public DbSet<Domain.Entities.Notification> Notifications => Set<Domain.Entities.Notification>();
        public DbSet<DeviceToken>                  DeviceTokens  => Set<DeviceToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── Notification ────────────────────────────────────────────────────
            modelBuilder.Entity<Domain.Entities.Notification>(e =>
            {
                e.HasKey(n => n.Id);

                e.Property(n => n.Message).HasMaxLength(500).IsRequired();
                e.Property(n => n.Type).HasConversion<string>();
                e.Property(n => n.Status).HasConversion<string>();

                e.HasQueryFilter(n => !n.IsDeleted);

                e.HasIndex(n => n.RecipientId);
                e.HasIndex(n => new { n.RecipientId, n.Status });
            });

            // ── DeviceToken ─────────────────────────────────────────────────────
            modelBuilder.Entity<DeviceToken>(e =>
            {
                e.HasKey(d => d.Id);

                e.Property(d => d.Token).HasMaxLength(512).IsRequired();
                e.Property(d => d.Platform).HasMaxLength(20).IsRequired();

                e.HasIndex(d => d.UserId);
                e.HasIndex(d => d.Token).IsUnique();
            });
        }
    }
}
