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
        Task<IEnumerable<Entities.Post>> GetUserPostsAsync(Guid userId, int page, int pageSize);
        Task<IEnumerable<Entities.Post>> GetFeedPostsAsync(List<Guid> userIds, int page, int pageSize);

        //Remember implement this function in application service and api
        Task<IEnumerable<Entities.Post>> GetFeedPostsCursorAsync(List<Guid> userIds, DateTime? lastPostCreatedAt,  Guid? lastPostId, int pageSize);
        Task<IEnumerable<Entities.Post>> GetAllPostsAsync(int page, int pageSize);
        Task<Entities.Post> AddAsync(Entities.Post post);
        Task UpdateAsync(Entities.Post post);
        Task DeleteAsync(Guid id);
        Task<int> GetUserPostsCountAsync(Guid userId);
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

    public interface IUnitOfWork : IDisposable
    {
        IPostRepository Posts { get; }
        ICommentRepository Comments { get; }
        IPostLikeRepository PostLikes { get; }
        Task<int> SaveChangesAsync();
        Task OpenConnectionAsync();
        Task CloseConnectionAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}