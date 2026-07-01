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

        public async Task<IEnumerable<Domain.Entities.Post>> GetUserPostsAsync(
            Guid userId, int page, int pageSize,
            Guid? currentUserId = null, bool isFriend = false)
        {
            var query = _context.Posts.Where(p => p.UserId == userId);

            // Owner thấy tất cả bài của mình
            if (currentUserId != userId)
            {
                query = isFriend
                    ? query.Where(p => p.Visibility == PostVisibility.Public || p.Visibility == PostVisibility.Friends)
                    : query.Where(p => p.Visibility == PostVisibility.Public);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Domain.Entities.Post>> GetFeedPostsAsync(List<Guid> userIds, Guid currentUserId, int page, int pageSize)
        {
            return await _context.Posts
            .Where(p => userIds.Contains(p.UserId) && (p.Visibility == PostVisibility.Public || p.UserId == currentUserId))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page-1)*pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetFeedPostsCountAsync(List<Guid> userIds, Guid currentUserId)
        {
            return await _context.Posts
            .Where(p => userIds.Contains(p.UserId) && (p.Visibility == PostVisibility.Public || p.UserId == currentUserId))
            .CountAsync();
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

        public async Task<IEnumerable<Domain.Entities.Post>> SearchPostsAsync(string query, int page, int pageSize)
        {
            var pattern = $"%{query}%";
            return await _context.Posts
                .Where(p => p.Visibility == PostVisibility.Public && EF.Functions.ILike(p.Content, pattern))
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountSearchPostsAsync(string query)
        {
            var pattern = $"%{query}%";
            return await _context.Posts
                .Where(p => p.Visibility == PostVisibility.Public && EF.Functions.ILike(p.Content, pattern))
                .CountAsync();
        }

        public async Task<int> GetPublicPostsCountAsync()
        {
            return await _context.Posts
                .Where(p => p.Visibility == PostVisibility.Public)
                .CountAsync();
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

        public async Task<Domain.Entities.Post?> GetByIdIgnoringFiltersAsync(Guid id)
        {
            return await _context.Posts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Domain.Entities.Post>> GetByIdsIgnoringFiltersAsync(List<Guid> ids)
        {
            if (ids.Count == 0) return new List<Domain.Entities.Post>();
            return await _context.Posts
                .IgnoreQueryFilters()
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();
        }

        public async Task<int> GetUserPostsCountAsync(
            Guid userId, Guid? currentUserId = null, bool isFriend = false)
        {
            var query = _context.Posts.Where(p => p.UserId == userId);

            if (currentUserId != userId)
            {
                query = isFriend
                    ? query.Where(p => p.Visibility == PostVisibility.Public || p.Visibility == PostVisibility.Friends)
                    : query.Where(p => p.Visibility == PostVisibility.Public);
            }

            return await query.CountAsync();
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

    public class ReportRepository : IReportRepository
    {
        private readonly PostDbContext _context;

        public ReportRepository(PostDbContext context)
        {
            _context = context;
        }

        public async Task<PostReport?> GetPendingByPostAndReporterAsync(Guid postId, Guid reporterId)
        {
            return await _context.PostReports.FirstOrDefaultAsync(r =>
                r.PostId == postId &&
                r.ReporterId == reporterId &&
                r.Status == ReportStatus.Pending);
        }

        public async Task<PostReport?> GetByIdAsync(Guid id)
        {
            return await _context.PostReports.FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<PostReport> AddAsync(PostReport report)
        {
            await _context.PostReports.AddAsync(report);
            return report;
        }

        public Task UpdateAsync(PostReport report)
        {
            _context.PostReports.Update(report);
            return Task.CompletedTask;
        }

        public async Task<List<PostReport>> GetReportsAsync(ReportStatus? status, int page, int pageSize)
        {
            var query = _context.PostReports.AsQueryable();
            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountReportsAsync(ReportStatus? status)
        {
            var query = _context.PostReports.AsQueryable();
            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);
            return await query.CountAsync();
        }

        public async Task<List<PostReport>> GetPendingByPostAsync(Guid postId)
        {
            return await _context.PostReports
                .Where(r => r.PostId == postId && r.Status == ReportStatus.Pending)
                .ToListAsync();
        }
    }

    public class SavedPostRepository : ISavedPostRepository
    {
        private readonly PostDbContext _context;

        public SavedPostRepository(PostDbContext context)
        {
            _context = context;
        }

        public async Task<SavedPost?> GetByPostAndUserAsync(Guid postId, Guid userId)
        {
            return await _context.SavedPosts
                .FirstOrDefaultAsync(s => s.PostId == postId && s.UserId == userId);
        }

        public async Task<SavedPost?> GetByPostAndUserIncludingDeletedAsync(Guid postId, Guid userId)
        {
            return await _context.SavedPosts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.PostId == postId && s.UserId == userId);
        }

        public async Task<bool> HasUserSavedPostAsync(Guid postId, Guid userId)
        {
            return await _context.SavedPosts
                .AnyAsync(s => s.PostId == postId && s.UserId == userId);
        }

        public async Task<SavedPost> AddAsync(SavedPost saved)
        {
            await _context.SavedPosts.AddAsync(saved);
            return saved;
        }

        public Task UpdateAsync(SavedPost saved)
        {
            _context.SavedPosts.Update(saved);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<Post.Domain.Entities.Post>> GetSavedPostsByUserAsync(Guid userId, int page, int pageSize)
        {
            // Join saved bookmarks (newest first) with their posts; the Post query filter
            // automatically excludes soft-deleted posts.
            return await _context.SavedPosts
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Include(s => s.Post)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => s.Post)
                .Where(p => p != null)
                .ToListAsync();
        }

        public async Task<int> CountSavedByUserAsync(Guid userId)
        {
            return await _context.SavedPosts
                .Where(s => s.UserId == userId)
                .CountAsync(s => s.Post != null);
        }
    }

    public class CommentLikeRepository : ICommentLikeRepository
    {
        private readonly PostDbContext _context;

        public CommentLikeRepository(PostDbContext context)
        {
            _context = context;
        }

        public async Task<CommentLike?> GetByCommentAndUserAsync(Guid commentId, Guid userId)
        {
            return await _context.CommentLikes
                .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);
        }

        public async Task<CommentLike?> GetByCommentAndUserIncludingDeletedAsync(Guid commentId, Guid userId)
        {
            return await _context.CommentLikes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);
        }

        public async Task<bool> HasUserLikedCommentAsync(Guid commentId, Guid userId)
        {
            return await _context.CommentLikes
                .AnyAsync(l => l.CommentId == commentId && l.UserId == userId);
        }

        public async Task<CommentLike> AddAsync(CommentLike like)
        {
            await _context.CommentLikes.AddAsync(like);
            return like;
        }

        public Task UpdateAsync(CommentLike like)
        {
            _context.CommentLikes.Update(like);
            return Task.CompletedTask;
        }
    }

    public class BoardRepository : IBoardRepository
    {
        private readonly PostDbContext _context;

        public BoardRepository(PostDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BoardPost>> GetPostsAsync(
            BoardTag? tag, bool sortByHot, int page, int pageSize)
        {
            var query = _context.BoardPosts.Where(p => !p.IsDeleted);

            if (tag.HasValue)
                query = query.Where(p => p.Tag == tag.Value);

            // Hot sort phải load vào memory vì HotScore dùng DateTime.UtcNow (không translate sang SQL)
            if (sortByHot)
            {
                var all = await query.ToListAsync();
                return all.OrderByDescending(p => p.HotScore)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountPostsAsync(BoardTag? tag)
        {
            var query = _context.BoardPosts.Where(p => !p.IsDeleted);
            if (tag.HasValue)
                query = query.Where(p => p.Tag == tag.Value);
            return await query.CountAsync();
        }

        public async Task<BoardPost?> GetByIdAsync(Guid id)
        {
            return await _context.BoardPosts
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task<BoardPost> AddAsync(BoardPost post)
        {
            await _context.BoardPosts.AddAsync(post);
            return post;
        }

        public Task UpdateAsync(BoardPost post)
        {
            _context.BoardPosts.Update(post);
            return Task.CompletedTask;
        }

        public async Task<BoardVote?> GetVoteAsync(Guid boardPostId, Guid userId)
        {
            return await _context.BoardVotes
                .FirstOrDefaultAsync(v => v.BoardPostId == boardPostId && v.UserId == userId && !v.IsDeleted);
        }

        public async Task<BoardVote?> GetVoteIncludingDeletedAsync(Guid boardPostId, Guid userId)
        {
            return await _context.BoardVotes
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(v => v.BoardPostId == boardPostId && v.UserId == userId);
        }

        public async Task<BoardVote> AddVoteAsync(BoardVote vote)
        {
            await _context.BoardVotes.AddAsync(vote);
            return vote;
        }

        public Task UpdateVoteAsync(BoardVote vote)
        {
            _context.BoardVotes.Update(vote);
            return Task.CompletedTask;
        }

        public async Task<BoardComment> AddCommentAsync(BoardComment comment)
        {
            await _context.BoardComments.AddAsync(comment);
            return comment;
        }

        public async Task<IEnumerable<BoardComment>> GetCommentsAsync(Guid boardPostId)
        {
            return await _context.BoardComments
                .Where(c => c.BoardPostId == boardPostId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<BoardComment?> GetCommentByIdAsync(Guid commentId)
        {
            return await _context.BoardComments
                .FirstOrDefaultAsync(c => c.Id == commentId);
        }

        public Task UpdateCommentAsync(BoardComment comment)
        {
            _context.BoardComments.Update(comment);
            return Task.CompletedTask;
        }
    }
}