using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Post.Application.DTOs;
using Post.Application.Interfaces;
using Post.Domain.Entities;
using Post.Domain.Interfaces;

namespace Post.Application.Services
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserProfileHttpClient _userProfileClient;
        private readonly IMessagePublisher _messagePublisher;

        public CommentService(
            IUnitOfWork unitOfWork,
            IUserProfileHttpClient userProfileClient,
            IMessagePublisher messagePublisher)
        {
            _unitOfWork = unitOfWork;
            _userProfileClient = userProfileClient;
            _messagePublisher = messagePublisher;
        }

        public async Task<ApiResponse<CommentDto>> GetCommentByIdAsync(Guid commentId)
        {
            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                
                if (comment == null || comment.IsDeleted)
                    return ApiResponse<CommentDto>.ErrorResponse("Comment not found");

                var commentDto = await MapToCommentDtoAsync(comment);
                return ApiResponse<CommentDto>.SuccessResponse(commentDto);
            }
            catch (Exception ex)
            {
                return ApiResponse<CommentDto>.ErrorResponse($"Error retrieving comment: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<CommentDto>>> GetPostCommentsAsync(Guid postId, Guid? currentUserId = null)
        {
            try
            {
                var comments = await _unitOfWork.Comments.GetPostCommentsAsync(postId);

                var commentDtos = new List<CommentDto>();
                var userIds = comments.Select(c => c.UserId).Distinct().ToList();
                var users = await _userProfileClient.GetUserProfilesAsync(userIds);

                foreach (var comment in comments.Where(c => !c.IsDeleted))
                {
                    var user = users.FirstOrDefault(u => u.UserId == comment.UserId);
                    var isLiked = currentUserId.HasValue &&
                        await _unitOfWork.CommentLikes.HasUserLikedCommentAsync(comment.Id, currentUserId.Value);

                    commentDtos.Add(new CommentDto
                    {
                        Id = comment.Id,
                        PostId = comment.PostId,
                        UserId = comment.UserId,
                        User = user ?? new UserProfileDto { Id = comment.UserId, Name = "Unknown User", UserName = "unknown" },
                        Content = comment.Content,
                        ParentCommentId = comment.ParentCommentId,
                        CreatedAt = comment.CreatedAt,
                        UpdatedAt = comment.UpdatedAt,
                        LikesCount = comment.LikesCount,
                        IsLikedByCurrentUser = isLiked
                    });
                }

                return ApiResponse<List<CommentDto>>.SuccessResponse(commentDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<CommentDto>>.ErrorResponse($"Error retrieving comments: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CommentDto>> CreateCommentAsync(
            Guid postId, Guid userId, CreateCommentDto dto)
        {
            try
            {
                var post = await _unitOfWork.Posts.GetByIdAsync(postId);
                
                if (post == null || post.IsDeleted)
                    return ApiResponse<CommentDto>.ErrorResponse("Post not found");

                var comment = Comment.Create(postId, userId, dto.Content, dto.ParentCommentId);
                
                await _unitOfWork.Comments.AddAsync(comment);
                post.AddComment(comment);
                await _unitOfWork.Posts.UpdateAsync(post);
                await _unitOfWork.SaveChangesAsync();

                // Publish event
                // await _messagePublisher.PublishCommentCreatedAsync(comment.Id, postId, userId, comment.Content);

                var commentDto = await MapToCommentDtoAsync(comment);
                return ApiResponse<CommentDto>.SuccessResponse(commentDto, "Comment created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<CommentDto>.ErrorResponse($"Error creating comment: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CommentDto>> UpdateCommentAsync(
            Guid commentId, Guid userId, UpdateCommentDto dto)
        {
            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                
                if (comment == null || comment.IsDeleted)
                    return ApiResponse<CommentDto>.ErrorResponse("Comment not found");

                if (!comment.CanBeEditedBy(userId))
                    return ApiResponse<CommentDto>.ErrorResponse("You don't have permission to edit this comment");

                comment.UpdateContent(dto.Content);
                await _unitOfWork.Comments.UpdateAsync(comment);
                await _unitOfWork.SaveChangesAsync();

                var commentDto = await MapToCommentDtoAsync(comment);
                return ApiResponse<CommentDto>.SuccessResponse(commentDto, "Comment updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<CommentDto>.ErrorResponse($"Error updating comment: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteCommentAsync(Guid commentId, Guid userId)
        {
            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                
                if (comment == null || comment.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("Comment not found");

                if (!comment.CanBeDeletedBy(userId))
                    return ApiResponse<bool>.ErrorResponse("You don't have permission to delete this comment");

                var post = await _unitOfWork.Posts.GetByIdAsync(comment.PostId);
                if (post != null)
                {
                    post.RemoveComment(commentId);
                    await _unitOfWork.Posts.UpdateAsync(post);
                }

                comment.SoftDelete();
                await _unitOfWork.Comments.UpdateAsync(comment);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true, "Comment deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error deleting comment: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> LikeCommentAsync(Guid commentId, Guid userId)
        {
            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null || comment.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("Comment not found");

                var existing = await _unitOfWork.CommentLikes.GetByCommentAndUserIncludingDeletedAsync(commentId, userId);
                if (existing != null && !existing.IsDeleted)
                    return ApiResponse<bool>.SuccessResponse(true, "Already liked");

                if (existing != null)
                    existing.RestoreLike();
                else
                    await _unitOfWork.CommentLikes.AddAsync(CommentLike.Create(commentId, userId));

                await _unitOfWork.SaveChangesAsync();

                comment.IncrementLikesCount();
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true, "Comment liked");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error liking comment: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UnlikeCommentAsync(Guid commentId, Guid userId)
        {
            try
            {
                var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
                if (comment == null || comment.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("Comment not found");

                var existing = await _unitOfWork.CommentLikes.GetByCommentAndUserAsync(commentId, userId);
                if (existing == null)
                    return ApiResponse<bool>.ErrorResponse("You have not liked this comment");

                existing.SoftDelete();
                await _unitOfWork.SaveChangesAsync();

                comment.DecrementLikesCount();
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(false, "Comment unliked");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error unliking comment: {ex.Message}");
            }
        }

        private async Task<CommentDto> MapToCommentDtoAsync(Comment comment, Guid? currentUserId = null)
        {
            var userProfile = await _userProfileClient.GetUserProfileAsync(comment.UserId);

            userProfile ??= new UserProfileDto { Id = comment.UserId, Name = "Unknown User", UserName = "unknown", FullName = "Unknown User" };

            var isLiked = currentUserId.HasValue &&
                await _unitOfWork.CommentLikes.HasUserLikedCommentAsync(comment.Id, currentUserId.Value);

            return new CommentDto
            {
                Id = comment.Id,
                PostId = comment.PostId,
                UserId = comment.UserId,
                User = userProfile,
                Content = comment.Content,
                ParentCommentId = comment.ParentCommentId,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                LikesCount = comment.LikesCount,
                IsLikedByCurrentUser = isLiked
            };
        }
    }
}