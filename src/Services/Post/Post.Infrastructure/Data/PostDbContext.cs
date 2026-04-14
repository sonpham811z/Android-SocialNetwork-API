using Microsoft.EntityFrameworkCore;
using Post.Domain.Entities;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Post.Infrastructure.Data
{
    public class PostDbContext : DbContext
    {
        public PostDbContext(DbContextOptions<PostDbContext> options) : base(options)
        {
        }

        public DbSet<Domain.Entities.Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("Post");

            // Post Configuration
            modelBuilder.Entity<Domain.Entities.Post>(entity =>
            {
                entity.ToTable("Posts");
                entity.HasKey(p => p.Id);

                entity.Property(p => p.UserId).IsRequired();
                entity.Property(p => p.Content).IsRequired().HasMaxLength(5000);
                entity.Property(p => p.Type).IsRequired();
                entity.Property(p => p.ImageUrl).HasMaxLength(500);
                entity.Property(p => p.ImagePublicId).HasMaxLength(200);
                entity.Property(p => p.AudioUrl).HasMaxLength(500);
                entity.Property(p => p.AudioPublicId).HasMaxLength(200);
                entity.Property(p => p.AudioDuration).HasMaxLength(20);
                
                // Store waveform as JSON
                entity.Property(p => p.Waveform)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<double>>(v, (JsonSerializerOptions)null)
                    )
                    .HasColumnType("TEXT")
                    .Metadata.SetValueComparer(new ValueComparer<List<double>>(
                        (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                        c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
                        c => c != null ? c.ToList() : null
                    ));

                entity.Property(p => p.Visibility).IsRequired();
                entity.Property(p => p.CreatedAt).IsRequired();
                
                entity.HasIndex(p => p.UserId);
                entity.HasIndex(p => p.CreatedAt);
                entity.HasIndex(p => new { p.UserId, p.CreatedAt });

                // Relationships
                entity.HasMany(p => p.Comments)
                    .WithOne(c => c.Post)
                    .HasForeignKey(c => c.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Likes)
                    .WithOne(l => l.Post)
                    .HasForeignKey(l => l.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Query filters for soft delete
                entity.HasQueryFilter(p => !p.IsDeleted);
            });

            // Comment Configuration
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.ToTable("Comments");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.PostId).IsRequired();
                entity.Property(c => c.UserId).IsRequired();
                entity.Property(c => c.Content).IsRequired().HasMaxLength(2000);
                entity.Property(c => c.CreatedAt).IsRequired();

                entity.HasIndex(c => c.PostId);
                entity.HasIndex(c => c.UserId);
                entity.HasIndex(c => c.ParentCommentId);

                // Self-referencing relationship for nested comments
                entity.HasOne(c => c.ParentComment)
                    .WithMany()
                    .HasForeignKey(c => c.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Query filters for soft delete
                entity.HasQueryFilter(c => !c.IsDeleted);
            });

            // PostLike Configuration
            modelBuilder.Entity<PostLike>(entity =>
            {
                entity.ToTable("PostLikes");
                entity.HasKey(l => l.Id);

                entity.Property(l => l.PostId).IsRequired();
                entity.Property(l => l.UserId).IsRequired();
                entity.Property(l => l.CreatedAt).IsRequired();

                // Partial unique index: cho phép re-like sau khi unlike (soft delete)
                entity.HasIndex(l => new { l.PostId, l.UserId })
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");
                entity.HasIndex(l => l.UserId);

                // Query filters for soft delete
                entity.HasQueryFilter(l => !l.IsDeleted);
            });
        }
    }
}