using Friend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Friend.Infrastructure.Data
{
    public class FriendDbContext : DbContext
    {
        public FriendDbContext(DbContextOptions<FriendDbContext> options) : base(options) { }

        public DbSet<Friendship> Friendships => Set<Friendship>();
        public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
        public DbSet<Follow> Follows => Set<Follow>();
        public DbSet<Block> Blocks => Set<Block>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("Friend");

            // ── Friendship ──────────────────────────────────────────────────────
            modelBuilder.Entity<Friendship>(e =>
            {
                e.HasKey(f => f.Id);
                e.Property(f => f.UserId1).IsRequired();
                e.Property(f => f.UserId2).IsRequired();
                e.Property(f => f.IsDeleted).HasDefaultValue(false);

                // Unique constraint on canonical pair to prevent duplicates at DB level
                e.HasIndex(f => new { f.UserId1, f.UserId2 }).IsUnique();

                // Filter out soft-deleted rows globally
                e.HasQueryFilter(f => !f.IsDeleted);
            });

            // ── FriendRequest ───────────────────────────────────────────────────
            modelBuilder.Entity<FriendRequest>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.SenderId).IsRequired();
                e.Property(r => r.ReceiverId).IsRequired();
                e.Property(r => r.Status)
                 .HasConversion<int>()
                 .HasDefaultValue(FriendRequestStatus.Pending);

                e.HasIndex(r => new { r.SenderId, r.ReceiverId });
                e.HasIndex(r => r.ReceiverId);
            });

            // ── Follow ──────────────────────────────────────────────────────────
            modelBuilder.Entity<Follow>(e =>
            {
                e.HasKey(f => f.Id);
                e.Property(f => f.FollowerId).IsRequired();
                e.Property(f => f.FolloweeId).IsRequired();
                e.Property(f => f.IsDeleted).HasDefaultValue(false);

                e.HasIndex(f => new { f.FollowerId, f.FolloweeId });
                e.HasIndex(f => f.FolloweeId);

                e.HasQueryFilter(f => !f.IsDeleted);
            });

            // ── Block ───────────────────────────────────────────────────────────
            modelBuilder.Entity<Block>(e =>
            {
                e.HasKey(b => b.Id);
                e.Property(b => b.BlockerId).IsRequired();
                e.Property(b => b.BlockedId).IsRequired();
                e.Property(b => b.IsDeleted).HasDefaultValue(false);

                e.HasIndex(b => new { b.BlockerId, b.BlockedId }).IsUnique();

                e.HasQueryFilter(b => !b.IsDeleted);
            });
        }
    }
}