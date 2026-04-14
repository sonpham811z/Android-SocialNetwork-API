using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Post.Domain.Entities;
using Post.Domain.Interfaces;
using Post.Infrastructure.Data;

namespace Post.Infrastructure.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly PostDbContext _context;

        public PostRepository(PostDbContext context)
        {
            _context = context;
        }

        public async Task<Domain.Entities.Post?> GetByIdAsync(Guid id)
        {
            return await _context.Posts
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Domain.Entities.Post?> GetByIdWithCommentsAsync(Guid id)
        {
            return await _context.Posts
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Domain.Entities.Post?> GetByIdForLikeAsync(Guid id)
        {
            // Load Post với ALL Likes (kể cả soft-deleted) để có thể restore
            return await _context.Posts
                .Include(p => p.Likes.Where(l => true)) // Force load all
                .IgnoreQueryFilters() // Skip soft-delete filter để lấy deleted likes
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Domain.Entities.Post>> GetUserPostsAsync(Guid userId, int page, int pageSize)
        {
            return await _context.Posts
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Domain.Entities.Post>> GetFeedPostsAsync(List<Guid> userIds, int page, int pageSize)
        {
            return await _context.Posts
                .Where(p => userIds.Contains(p.UserId) && p.Visibility == PostVisibility.Public)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page-1)*pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Domain.Entities.Post>> GetFeedPostsCursorAsync(
            List<Guid> userIds, 
            DateTime? lastPostCreatedAt, // Thời gian của bài viết cuối cùng đang hiển thị
            Guid? lastPostId,            // ID của bài viết cuối cùng 
            int pageSize)
        {
            // 1. Khởi tạo Query gốc
            var query = _context.Posts
                .Where(p => userIds.Contains(p.UserId) && p.Visibility == PostVisibility.Public);

            // 2. Chèn logic đánh dấu mốc (Cursor)
            if (lastPostCreatedAt.HasValue && lastPostId.HasValue)
            {
                // Lấy những bài CŨ HƠN mốc thời gian truyền vào. 
                // Hoặc nếu đăng cùng 1 tíc tắc, thì xét ID nhỏ hơn để không bị trùng bài.
                query = query.Where(p => 
                    p.CreatedAt < lastPostCreatedAt.Value || 
                    (p.CreatedAt == lastPostCreatedAt.Value && p.Id.CompareTo(lastPostId.Value) < 0));
            }

            // 3. Sắp xếp và Lấy data
            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id) // Bắt buộc phải kẹp thêm ID để sắp xếp nhất quán 100%
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Domain.Entities.Post>> GetAllPostsAsync(int page, int pageSize)
        {
            return await _context.Posts
                .Where(p => p.Visibility == PostVisibility.Public)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Domain.Entities.Post> AddAsync(Domain.Entities.Post post)
        {
            await _context.Posts.AddAsync(post);
            return post;
        }

        public Task UpdateAsync(Domain.Entities.Post post)
        {
            _context.Posts.Update(post);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            var post = await GetByIdAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
            }
        }

        public async Task<int> GetUserPostsCountAsync(Guid userId)
        {
            return await _context.Posts
                .Where(p => p.UserId == userId)
                .CountAsync();
        }
    }

    public class CommentRepository : ICommentRepository
    {
        private readonly PostDbContext _context;

        public CommentRepository(PostDbContext context)
        {
            _context = context;
        }

        public async Task<Comment?> GetByIdAsync(Guid id)
        {
            return await _context.Comments
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Comment>> GetPostCommentsAsync(Guid postId)
        {
            return await _context.Comments
                .Where(c => c.PostId == postId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Comment> AddAsync(Comment comment)
        {
            await _context.Comments.AddAsync(comment);
            return comment;
        }

        public Task UpdateAsync(Comment comment)
        {
            _context.Comments.Update(comment);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            var comment = await GetByIdAsync(id);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
            }
        }
    }

    public class PostLikeRepository : IPostLikeRepository
    {
        private readonly PostDbContext _context;

        public PostLikeRepository(PostDbContext context)
        {
            _context = context;
        }

        public async Task<PostLike?> GetByIdAsync(Guid id)
        {
            return await _context.PostLikes
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<PostLike?> GetByPostAndUserAsync(Guid postId, Guid userId)
        {
            return await _context.PostLikes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        }

        public async Task<PostLike?> GetByPostAndUserIncludingDeletedAsync(Guid postId, Guid userId)
        {
            return await _context.PostLikes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        }

        public async Task<IEnumerable<PostLike>> GetPostLikesAsync(Guid postId)
        {
            return await _context.PostLikes
                .Where(l => l.PostId == postId)
                .ToListAsync();
        }

        public async Task<bool> HasUserLikedPostAsync(Guid postId, Guid userId)
        {
            return await _context.PostLikes
                .AnyAsync(l => l.PostId == postId && l.UserId == userId);
        }

        public async Task<PostLike> AddAsync(PostLike like)
        {
            await _context.PostLikes.AddAsync(like);
            return like;
        }

        public Task UpdateAsync(PostLike like)
        {
            _context.PostLikes.Update(like);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            var like = await GetByIdAsync(id);
            if (like != null)
            {
                _context.PostLikes.Remove(like);
            }
        }
    }
}