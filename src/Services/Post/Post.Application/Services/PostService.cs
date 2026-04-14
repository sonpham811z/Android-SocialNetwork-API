using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Post.Application.DTOs;
using Post.Application.Interfaces;
using Post.Domain.Entities;
using Post.Domain.Interfaces;

namespace Post.Application.Services
{
    public class PostService:IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediaService _mediaService;
        private readonly IUserProfileHttpClient _userProfileClient;
        private readonly IMessagePublisher _messagePublisher;

        public PostService(
            IUnitOfWork unitOfWork,
            IMediaService mediaService,
            IUserProfileHttpClient userProfileClient,
            IMessagePublisher messagePublisher)
        {
            _unitOfWork = unitOfWork;
            _mediaService = mediaService;
            _userProfileClient = userProfileClient;
            _messagePublisher = messagePublisher;
        }

        public async Task<ApiResponse<PostDto>> GetPostByIdAsync(Guid postId, Guid? currentUserId = null)
        {
            try
            {
                var post = await _unitOfWork.Posts.GetByIdWithCommentsAsync(postId);
                
                if (post == null || post.IsDeleted)
                    return ApiResponse<PostDto>.ErrorResponse("Post not found");

                var postDto = await MapToPostDtoAsync(post, currentUserId);
                
                return ApiResponse<PostDto>.SuccessResponse(postDto);
            }
            catch (Exception ex)
            {
                return ApiResponse<PostDto>.ErrorResponse($"Error retrieving post: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PaginatedResponse<PostDto>>> GetUserPostsAsync(
            Guid userId, int page, int pageSize, Guid? currentUserId = null)
        {
            try
            {
                var posts = await _unitOfWork.Posts.GetUserPostsAsync(userId, page, pageSize);
                var totalCount = await _unitOfWork.Posts.GetUserPostsCountAsync(userId);
                
                var postDtos = new List<PostDto>();
                foreach (var post in posts)
                {
                    var dto = await MapToPostDtoAsync(post, currentUserId);
                    postDtos.Add(dto);
                }

                var response = new PaginatedResponse<PostDto>
                {
                    Items = postDtos,
                    TotalItems = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    HasNextPage = page * pageSize < totalCount,
                    HasPreviousPage = page > 1
                };

                return ApiResponse<PaginatedResponse<PostDto>>.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<PaginatedResponse<PostDto>>.ErrorResponse($"Error retrieving posts: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PaginatedResponse<PostDto>>> GetFeedAsync(
            Guid currentUserId, int page, int pageSize)
        {
            try
            {
                // TODO: Get user's friends/following list from User Service
                // For now, just get all public posts
                var posts = await _unitOfWork.Posts.GetAllPostsAsync(page, pageSize);
                
                var postDtos = new List<PostDto>();
                foreach (var post in posts)
                {
                    var dto = await MapToPostDtoAsync(post, currentUserId);
                    postDtos.Add(dto);
                }

                var response = new PaginatedResponse<PostDto>
                {
                    Items = postDtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = 1,
                    HasNextPage = false,
                    HasPreviousPage = page > 1
                };

                return ApiResponse<PaginatedResponse<PostDto>>.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<PaginatedResponse<PostDto>>.ErrorResponse($"Error retrieving feed: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PostDto>> CreateTextPostAsync(Guid userId, CreateTextPostDto dto)
        {
            try
            {
                var visibility = Enum.Parse<PostVisibility>(dto.Visibility, true);
                var post = Domain.Entities.Post.CreateTextPost(userId, dto.Content, visibility);
                
                await _unitOfWork.Posts.AddAsync(post);
                await _unitOfWork.SaveChangesAsync();

                // Publish event
                await _messagePublisher.PublishPostCreatedAsync(post.Id, userId, post.Content);

                // Update user posts count
                await _userProfileClient.UpdatePostsCountAsync(userId, 1);

                var postDto = await MapToPostDtoAsync(post);
                return ApiResponse<PostDto>.SuccessResponse(postDto, "Post created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<PostDto>.ErrorResponse($"Error creating post: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PostDto>> CreateImagePostAsync(
            Guid userId, CreateImagePostDto dto, IFormFile image)
        {
            try
            {
                // Upload image to Cloudinary
                var (imageUrl, publicId) = await _mediaService.UploadImageAsync(image);
                
                var visibility = Enum.Parse<PostVisibility>(dto.Visibility, true);
                var post = Domain.Entities.Post.CreateImagePost(userId, dto.Content, imageUrl, publicId, visibility);
                
                await _unitOfWork.Posts.AddAsync(post);
                await _unitOfWork.SaveChangesAsync();

                // Publish event
                await _messagePublisher.PublishPostCreatedAsync(post.Id, userId, post.Content);

                // Update user posts count
                await _userProfileClient.UpdatePostsCountAsync(userId, 1);

                var postDto = await MapToPostDtoAsync(post);
                return ApiResponse<PostDto>.SuccessResponse(postDto, "Image post created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<PostDto>.ErrorResponse($"Error creating image post: {ex.Message}");
            }
        }

         public async Task<ApiResponse<PostDto>> CreateVoicePostAsync(
            Guid userId, CreateVoicePostDto dto, IFormFile audio)
        {
            try
            {
                // Upload audio and get waveform
                var (audioUrl, publicId, duration, waveform) = await _mediaService.UploadAudioAsync(audio);
                
                var visibility = Enum.Parse<PostVisibility>(dto.Visibility, true);
                var post = Domain.Entities.Post.CreateVoicePost(
                    userId, dto.Content, audioUrl, publicId, duration, waveform, visibility);
                
                await _unitOfWork.Posts.AddAsync(post);
                await _unitOfWork.SaveChangesAsync();

                // Publish event
                await _messagePublisher.PublishPostCreatedAsync(post.Id, userId, post.Content);

                // Update user posts count
                await _userProfileClient.UpdatePostsCountAsync(userId, 1);

                var postDto = await MapToPostDtoAsync(post);
                return ApiResponse<PostDto>.SuccessResponse(postDto, "Voice post created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<PostDto>.ErrorResponse($"Error creating voice post: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PostDto>> UpdatePostAsync(Guid postId, Guid userId, UpdatePostDto dto)
        {
            try
            {
                var post = await _unitOfWork.Posts.GetByIdAsync(postId);
                
                if (post == null || post.IsDeleted)
                    return ApiResponse<PostDto>.ErrorResponse("Post not found");

                if (!post.CanBeEditedBy(userId))
                    return ApiResponse<PostDto>.ErrorResponse("You don't have permission to edit this post");

                post.UpdateContent(dto.Content);
                
                if (!string.IsNullOrEmpty(dto.Visibility))
                {
                    var visibility = Enum.Parse<PostVisibility>(dto.Visibility, true);
                    post.UpdateVisibility(visibility);
                }

                await _unitOfWork.Posts.UpdateAsync(post);
                await _unitOfWork.SaveChangesAsync();

                var postDto = await MapToPostDtoAsync(post);
                return ApiResponse<PostDto>.SuccessResponse(postDto, "Post updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<PostDto>.ErrorResponse($"Error updating post: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeletePostAsync(Guid postId, Guid userId)
        {
            try
            {
                var post = await _unitOfWork.Posts.GetByIdAsync(postId);
                
                if (post == null || post.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("Post not found");

                if (!post.CanBeDeletedBy(userId))
                    return ApiResponse<bool>.ErrorResponse("You don't have permission to delete this post");

                // Delete media if exists
                if (!string.IsNullOrEmpty(post.ImagePublicId))
                    await _mediaService.DeleteImageAsync(post.ImagePublicId);
                    
                if (!string.IsNullOrEmpty(post.AudioPublicId))
                    await _mediaService.DeleteAudioAsync(post.AudioPublicId);

                post.SoftDelete();
                await _unitOfWork.Posts.UpdateAsync(post);
                await _unitOfWork.SaveChangesAsync();

                // Publish event
                await _messagePublisher.PublishPostDeletedAsync(postId, userId);

                // Update user posts count
                await _userProfileClient.UpdatePostsCountAsync(userId, -1);

                return ApiResponse<bool>.SuccessResponse(true, "Post deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error deleting post: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> LikePostAsync(Guid postId, Guid userId)
        {
            try
            {
                var post = await _unitOfWork.Posts.GetByIdAsync(postId);
                if (post == null || post.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("Post not found");

                var existingLike = await _unitOfWork.PostLikes.GetByPostAndUserIncludingDeletedAsync(postId, userId);
                if (existingLike != null && !existingLike.IsDeleted)
                    return ApiResponse<bool>.SuccessResponse(true, "Post already liked");

                if (existingLike != null)
                {
                    existingLike.RestoreLike();
                }
                else
                {
                    var newLike = PostLike.Create(postId, userId);
                    await _unitOfWork.PostLikes.AddAsync(newLike);
                }
                await _unitOfWork.SaveChangesAsync(); 

                post.IncrementLikesCount();
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true, "Post liked successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error liking post: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UnlikePostAsync(Guid postId, Guid userId)
        {
            try
            {
                var post = await _unitOfWork.Posts.GetByIdAsync(postId);
                if (post == null || post.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("Post not found");

                var existingLike = await _unitOfWork.PostLikes.GetByPostAndUserAsync(postId, userId);
                if (existingLike == null)
                    return ApiResponse<bool>.ErrorResponse("You have not liked this post");

                existingLike.SoftDelete();
                await _unitOfWork.SaveChangesAsync();

                post.DecrementLikesCount();
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true, "Post unliked successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error unliking post: {ex.Message}");
            }
        }

        // Helper methods
        private async Task<PostDto> MapToPostDtoAsync(Domain.Entities.Post post, Guid? currentUserId = null)
        {
            var userProfile = await _userProfileClient.GetUserProfileAsync(post.UserId);
            
            // Fallback to default UserProfileDto if not found
            if (userProfile == null)
            {
                userProfile = new UserProfileDto 
                { 
                    Id = post.UserId, 
                    Name = "Unknown User", 
                    UserName = "unknown",
                    FullName = "Unknown User"
                };
            }
            
            bool isLiked = false;
            if (currentUserId.HasValue)
            {
                isLiked = await _unitOfWork.PostLikes.HasUserLikedPostAsync(post.Id, currentUserId.Value);
            }

            var commentsDto = new List<CommentDto>();
            if (post.Comments != null && post.Comments.Any())
            {
                var commentUserIds = post.Comments.Select(c => c.UserId).Distinct().ToList();
                var commentUsers = await _userProfileClient.GetUserProfilesAsync(commentUserIds);
                
                foreach (var comment in post.Comments.Where(c => !c.IsDeleted))
                {
                    var commentUser = commentUsers.FirstOrDefault(u => u.Id == comment.UserId);
                    commentsDto.Add(new CommentDto
                    {
                        Id = comment.Id,
                        PostId = comment.PostId,
                        UserId = comment.UserId,
                        User = commentUser ?? new UserProfileDto { Id = comment.UserId, Name = "Unknown User", UserName = "unknown", FullName = "Unknown User" },
                        Content = comment.Content,
                        ParentCommentId = comment.ParentCommentId,
                        CreatedAt = comment.CreatedAt,
                        UpdatedAt = comment.UpdatedAt
                    });
                }
            }

            return new PostDto
            {
                Id = post.Id,
                UserId = post.UserId,
                User = userProfile,
                Content = post.Content,
                Type = post.Type.ToString(),
                ImageUrl = post.ImageUrl,
                AudioUrl = post.AudioUrl,
                AudioDuration = post.AudioDuration,
                Waveform = post.Waveform,
                LikesCount = post.LikesCount,
                CommentsCount = post.CommentsCount,
                SharesCount = post.SharesCount,
                Visibility = post.Visibility.ToString(),
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                IsLikedByCurrentUser = isLiked,
                Comments = commentsDto.Any() ? commentsDto : null
            };
        }
    }
}