using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Post.Domain.Entities;

namespace Post.Domain.Interfaces
{
    public interface IPostRepository
    {
        Task<Entities.Post?> GetByIdAsync(Guid id);
        Task<Entities.Post?> GetByIdWithCommentsAsync(Guid id);
        Task<Entities.Post?> GetByIdForLikeAsync(Guid id);
        Task<IEnumerable<Entities.Post>> GetUserPostsAsync(Guid userId, int page, int pageSize, Guid? currentUserId = null, bool isFriend = false);
        Task<IEnumerable<Entities.Post>> GetFeedPostsAsync(List<Guid> userIds, Guid currentUserId, int page, int pageSize);
        Task<int> GetFeedPostsCountAsync(List<Guid> userIds, Guid currentUserId);
        Task<IEnumerable<Entities.Post>> GetFeedPostsCursorAsync(List<Guid> userIds, DateTime? lastPostCreatedAt,  Guid? lastPostId, int pageSize);
        Task<IEnumerable<Entities.Post>> GetAllPostsAsync(int page, int pageSize);
        Task<Entities.Post> AddAsync(Entities.Post post);
        Task UpdateAsync(Entities.Post post);
        Task DeleteAsync(Guid id);
        Task<int> GetUserPostsCountAsync(Guid userId, Guid? currentUserId = null, bool isFriend = false);

        // Admin/moderation: lấy bài kể cả đã ẩn (bỏ qua query filter)
        Task<Entities.Post?> GetByIdIgnoringFiltersAsync(Guid id);
        Task<List<Entities.Post>> GetByIdsIgnoringFiltersAsync(List<Guid> ids);
    }

    public interface ICommentRepository
    {
        Task<Comment?> GetByIdAsync(Guid id);
        Task<IEnumerable<Comment>> GetPostCommentsAsync(Guid postId);
        Task<Comment> AddAsync(Comment comment);
        Task UpdateAsync(Comment comment);
        Task DeleteAsync(Guid id);
    }

    public interface IPostLikeRepository
    {
        Task<PostLike?> GetByIdAsync(Guid id);
        Task<PostLike?> GetByPostAndUserAsync(Guid postId, Guid userId);
        Task<PostLike?> GetByPostAndUserIncludingDeletedAsync(Guid postId, Guid userId);
        Task<IEnumerable<PostLike>> GetPostLikesAsync(Guid postId);
        Task<bool> HasUserLikedPostAsync(Guid postId, Guid userId);
        Task<PostLike> AddAsync(PostLike like);
        Task UpdateAsync(PostLike like);
        Task DeleteAsync(Guid id);
    }

    public interface ICommentLikeRepository
    {
        Task<CommentLike?> GetByCommentAndUserAsync(Guid commentId, Guid userId);
        Task<CommentLike?> GetByCommentAndUserIncludingDeletedAsync(Guid commentId, Guid userId);
        Task<bool> HasUserLikedCommentAsync(Guid commentId, Guid userId);
        Task<CommentLike> AddAsync(CommentLike like);
        Task UpdateAsync(CommentLike like);
    }

    public interface IReportRepository
    {
        Task<PostReport?> GetPendingByPostAndReporterAsync(Guid postId, Guid reporterId);
        Task<PostReport?> GetByIdAsync(Guid id);
        Task<PostReport> AddAsync(PostReport report);
        Task UpdateAsync(PostReport report);
        Task<List<PostReport>> GetReportsAsync(ReportStatus? status, int page, int pageSize);
        Task<int> CountReportsAsync(ReportStatus? status);
        Task<List<PostReport>> GetPendingByPostAsync(Guid postId);
    }

    public interface ISavedPostRepository
    {
        Task<SavedPost?> GetByPostAndUserAsync(Guid postId, Guid userId);
        Task<SavedPost?> GetByPostAndUserIncludingDeletedAsync(Guid postId, Guid userId);
        Task<bool> HasUserSavedPostAsync(Guid postId, Guid userId);
        Task<SavedPost> AddAsync(SavedPost saved);
        Task UpdateAsync(SavedPost saved);

        /// <summary>Posts a user has saved, newest-saved first, excluding deleted posts.</summary>
        Task<IEnumerable<Entities.Post>> GetSavedPostsByUserAsync(Guid userId, int page, int pageSize);
        Task<int> CountSavedByUserAsync(Guid userId);
    }

    public interface IStoryRepository
    {
        Task<Entities.Story?> GetByIdAsync(Guid id);
        Task<Entities.Story?> GetByIdWithViewsAsync(Guid id);
        Task<IEnumerable<Entities.Story>> GetUserActiveStoriesAsync(Guid userId);
        Task<IEnumerable<Entities.Story>> GetFeedStoriesAsync(List<Guid> userIds);
        Task<bool> HasUserViewedStoryAsync(Guid storyId, Guid viewerId);
        Task<Entities.Story> AddAsync(Entities.Story story);
        Task UpdateAsync(Entities.Story story);
        Task<Entities.StoryView> AddViewAsync(Entities.StoryView view);
    }

    public interface IBoardRepository
    {
        Task<IEnumerable<BoardPost>> GetPostsAsync(BoardTag? tag, bool sortByHot, int page, int pageSize);
        Task<int> CountPostsAsync(BoardTag? tag);
        Task<BoardPost?> GetByIdAsync(Guid id);
        Task<BoardPost> AddAsync(BoardPost post);
        Task UpdateAsync(BoardPost post);
        Task<BoardVote?> GetVoteAsync(Guid boardPostId, Guid userId);
        Task<BoardVote?> GetVoteIncludingDeletedAsync(Guid boardPostId, Guid userId);
        Task<BoardVote> AddVoteAsync(BoardVote vote);
        Task UpdateVoteAsync(BoardVote vote);

        Task<BoardComment> AddCommentAsync(BoardComment comment);
        Task<IEnumerable<BoardComment>> GetCommentsAsync(Guid boardPostId);
        Task<BoardComment?> GetCommentByIdAsync(Guid commentId);
        Task UpdateCommentAsync(BoardComment comment);
    }

    public interface IUnitOfWork : IDisposable
    {
        IPostRepository Posts { get; }
        ICommentRepository Comments { get; }
        IPostLikeRepository PostLikes { get; }
        ICommentLikeRepository CommentLikes { get; }
        ISavedPostRepository SavedPosts { get; }
        IReportRepository Reports { get; }
        IStoryRepository Stories { get; }
        IBoardRepository Board { get; }
        Task<int> SaveChangesAsync();
        Task OpenConnectionAsync();
        Task CloseConnectionAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}