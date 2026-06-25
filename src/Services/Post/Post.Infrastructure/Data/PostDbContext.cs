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
        public DbSet<CommentLike> CommentLikes { get; set; }
        public DbSet<SavedPost> SavedPosts { get; set; }
        public DbSet<PostReport> PostReports { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<StoryView> StoryViews { get; set; }
        public DbSet<BoardPost> BoardPosts { get; set; }
        public DbSet<BoardVote> BoardVotes { get; set; }
        public DbSet<BoardComment> BoardComments { get; set; }

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
                entity.Property(p => p.VideoUrl).HasMaxLength(500);
                entity.Property(p => p.VideoPublicId).HasMaxLength(200);
                entity.Property(p => p.VideoThumbnailUrl).HasMaxLength(500);
                
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
                entity.Property(p => p.IsHidden).HasDefaultValue(false);
                entity.Property(p => p.OriginalPostId).IsRequired(false);

                entity.HasIndex(p => p.UserId);
                entity.HasIndex(p => p.CreatedAt);
                entity.HasIndex(p => new { p.UserId, p.CreatedAt });
                entity.HasIndex(p => p.OriginalPostId);

                // Relationships
                entity.HasMany(p => p.Comments)
                    .WithOne(c => c.Post)
                    .HasForeignKey(c => c.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Likes)
                    .WithOne(l => l.Post)
                    .HasForeignKey(l => l.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Query filters: ẩn bài đã xoá (chủ post) hoặc bị admin ẩn
                entity.HasQueryFilter(p => !p.IsDeleted && !p.IsHidden);
            });

            // PostReport Configuration
            modelBuilder.Entity<PostReport>(entity =>
            {
                entity.ToTable("PostReports");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.PostId).IsRequired();
                entity.Property(r => r.ReporterId).IsRequired();
                entity.Property(r => r.Reason).IsRequired().HasMaxLength(500);
                entity.Property(r => r.Status).IsRequired();
                entity.Property(r => r.CreatedAt).IsRequired();
                entity.HasIndex(r => r.PostId);
                entity.HasIndex(r => r.Status);
                // Mỗi user chỉ 1 report đang chờ cho 1 bài (chặn spam report)
                entity.HasIndex(r => new { r.PostId, r.ReporterId, r.Status });
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
                entity.Property(c => c.LikesCount).IsRequired().HasDefaultValue(0);

                entity.HasIndex(c => c.PostId);
                entity.HasIndex(c => c.UserId);
                entity.HasIndex(c => c.ParentCommentId);

                entity.HasOne(c => c.ParentComment)
                    .WithMany()
                    .HasForeignKey(c => c.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(c => !c.IsDeleted);
            });

            // CommentLike Configuration
            modelBuilder.Entity<CommentLike>(entity =>
            {
                entity.ToTable("CommentLikes");
                entity.HasKey(l => l.Id);

                entity.Property(l => l.CommentId).IsRequired();
                entity.Property(l => l.UserId).IsRequired();
                entity.Property(l => l.CreatedAt).IsRequired();

                entity.HasIndex(l => new { l.CommentId, l.UserId })
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");
                entity.HasIndex(l => l.UserId);

                entity.HasOne(l => l.Comment)
                    .WithMany()
                    .HasForeignKey(l => l.CommentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasQueryFilter(l => !l.IsDeleted);
            });

            // Story Configuration
            modelBuilder.Entity<Story>(entity =>
            {
                entity.ToTable("Stories");
                entity.HasKey(s => s.Id);

                entity.Property(s => s.UserId).IsRequired();
                entity.Property(s => s.MediaUrl).HasMaxLength(500);
                entity.Property(s => s.MediaPublicId).HasMaxLength(200);
                entity.Property(s => s.ThumbnailUrl).HasMaxLength(500);
                entity.Property(s => s.ThumbnailPublicId).HasMaxLength(200);
                entity.Property(s => s.MediaType).IsRequired();
                entity.Property(s => s.CreatedAt).IsRequired();
                entity.Property(s => s.ExpiresAt).IsRequired();

                entity.HasIndex(s => s.UserId);
                entity.HasIndex(s => s.ExpiresAt);
                entity.HasIndex(s => new { s.UserId, s.ExpiresAt });

                entity.HasMany(s => s.Views)
                    .WithOne(v => v.Story)
                    .HasForeignKey(v => v.StoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasQueryFilter(s => !s.IsDeleted && s.ExpiresAt > DateTime.UtcNow);
            });

            // StoryView Configuration
            modelBuilder.Entity<StoryView>(entity =>
            {
                entity.ToTable("StoryViews");
                entity.HasKey(v => v.Id);

                entity.Property(v => v.StoryId).IsRequired();
                entity.Property(v => v.ViewerId).IsRequired();
                entity.Property(v => v.ViewedAt).IsRequired();

                entity.HasIndex(v => new { v.StoryId, v.ViewerId }).IsUnique();
                entity.HasIndex(v => v.ViewerId);
            });

            // BoardPost Configuration
            modelBuilder.Entity<BoardPost>(entity =>
            {
                entity.ToTable("BoardPosts");
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Content).IsRequired().HasMaxLength(2000);
                entity.Property(b => b.Tag).IsRequired();
                entity.Property(b => b.IsAnonymous).IsRequired();
                entity.Property(b => b.UpvotesCount).HasDefaultValue(0);
                entity.Property(b => b.DownvotesCount).HasDefaultValue(0);
                entity.Property(b => b.CommentsCount).HasDefaultValue(0);
                entity.Property(b => b.CreatedAt).IsRequired();
                entity.HasIndex(b => b.Tag);
                entity.HasIndex(b => b.CreatedAt);
                entity.HasIndex(b => b.AuthorId);
                entity.HasQueryFilter(b => !b.IsDeleted);
            });

            // BoardVote Configuration
            modelBuilder.Entity<BoardVote>(entity =>
            {
                entity.ToTable("BoardVotes");
                entity.HasKey(v => v.Id);
                entity.Property(v => v.BoardPostId).IsRequired();
                entity.Property(v => v.UserId).IsRequired();
                entity.Property(v => v.Type).IsRequired();
                entity.Property(v => v.CreatedAt).IsRequired();
                entity.HasIndex(v => new { v.BoardPostId, v.UserId })
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");
                entity.HasOne(v => v.BoardPost)
                    .WithMany()
                    .HasForeignKey(v => v.BoardPostId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasQueryFilter(v => !v.IsDeleted);
            });

            // BoardComment Configuration
            modelBuilder.Entity<BoardComment>(entity =>
            {
                entity.ToTable("BoardComments");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.BoardPostId).IsRequired();
                entity.Property(c => c.AuthorId).IsRequired();
                entity.Property(c => c.Content).IsRequired().HasMaxLength(1000);
                entity.Property(c => c.IsAnonymous).IsRequired();
                entity.Property(c => c.CreatedAt).IsRequired();
                entity.HasIndex(c => c.BoardPostId);
                entity.HasIndex(c => c.AuthorId);
                entity.HasOne(c => c.BoardPost)
                    .WithMany()
                    .HasForeignKey(c => c.BoardPostId)
                    .OnDelete(DeleteBehavior.Cascade);
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

            // SavedPost (bookmark) Configuration
            modelBuilder.Entity<SavedPost>(entity =>
            {
                entity.ToTable("SavedPosts");
                entity.HasKey(s => s.Id);

                entity.Property(s => s.PostId).IsRequired();
                entity.Property(s => s.UserId).IsRequired();
                entity.Property(s => s.CreatedAt).IsRequired();

                // Partial unique index: allow re-save after un-save (soft delete)
                entity.HasIndex(s => new { s.PostId, s.UserId })
                    .IsUnique()
                    .HasFilter("\"IsDeleted\" = false");
                entity.HasIndex(s => s.UserId);

                entity.HasOne(s => s.Post)
                    .WithMany()
                    .HasForeignKey(s => s.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasQueryFilter(s => !s.IsDeleted);
            });
        }
    }
}