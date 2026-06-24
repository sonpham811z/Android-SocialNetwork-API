using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Post.Application.DTOs;

namespace Post.Application.Interfaces
{
    public interface IPostService
    {
        Task<ApiResponse<PostDto>> GetPostByIdAsync(Guid postId, Guid? currentUserId = null);
        Task<ApiResponse<PaginatedResponse<PostDto>>> GetUserPostsAsync(Guid userId, int page, int pageSize, Guid? currentUserId = null);
        Task<ApiResponse<PaginatedResponse<PostDto>>> GetFeedAsync(Guid currentUserId, int page, int pageSize);
        Task<ApiResponse<PostDto>> CreateTextPostAsync(Guid userId, CreateTextPostDto dto);
        Task<ApiResponse<PostDto>> CreateImagePostAsync(Guid userId, CreateImagePostDto dto, IFormFile image);
        Task<ApiResponse<PostDto>> CreateVoicePostAsync(Guid userId, CreateVoicePostDto dto, IFormFile audio);
        Task<ApiResponse<PostDto>> CreateVideoPostAsync(Guid userId, CreateVideoPostDto dto, IFormFile video);
        Task<ApiResponse<PostDto>> SharePostAsync(Guid originalPostId, Guid userId, SharePostDto dto);
        Task<ApiResponse<PostDto>> UpdatePostAsync(Guid postId, Guid userId, UpdatePostDto dto);
        Task<ApiResponse<PostDto>> UpdatePostMediaAsync(Guid postId, Guid userId, string action, string? mediaType, IFormFile? file);
        Task<ApiResponse<bool>> DeletePostAsync(Guid postId, Guid userId);
        Task<ApiResponse<bool>> LikePostAsync(Guid postId, Guid userId);
        Task<ApiResponse<bool>> UnlikePostAsync(Guid postId, Guid userId);
        Task<ApiResponse<bool>> SavePostAsync(Guid postId, Guid userId);
        Task<ApiResponse<bool>> UnsavePostAsync(Guid postId, Guid userId);
    }

    public interface ICommentService
    {
        Task<ApiResponse<CommentDto>> GetCommentByIdAsync(Guid commentId);
        Task<ApiResponse<List<CommentDto>>> GetPostCommentsAsync(Guid postId, Guid? currentUserId = null);
        Task<ApiResponse<CommentDto>> CreateCommentAsync(Guid postId, Guid userId, CreateCommentDto dto);
        Task<ApiResponse<CommentDto>> UpdateCommentAsync(Guid commentId, Guid userId, UpdateCommentDto dto);
        Task<ApiResponse<bool>> DeleteCommentAsync(Guid commentId, Guid userId);
        Task<ApiResponse<bool>> LikeCommentAsync(Guid commentId, Guid userId);
        Task<ApiResponse<bool>> UnlikeCommentAsync(Guid commentId, Guid userId);
    }

    public interface IBoardService
    {
        Task<ApiResponse<PaginatedResponse<BoardPostDto>>> GetPostsAsync(string? tag, string sort, int page, int pageSize, Guid? currentUserId);
        Task<ApiResponse<BoardPostDto>> CreatePostAsync(Guid userId, CreateBoardPostDto dto);
        Task<ApiResponse<bool>> VoteAsync(Guid postId, Guid userId, string voteType);
        Task<ApiResponse<bool>> DeleteVoteAsync(Guid postId, Guid userId);
        Task<ApiResponse<bool>> DeletePostAsync(Guid postId, Guid userId);
        Task<ApiResponse<List<BoardCommentDto>>> GetCommentsAsync(Guid boardPostId, Guid? currentUserId);
        Task<ApiResponse<BoardCommentDto>> AddCommentAsync(Guid boardPostId, Guid userId, CreateBoardCommentDto dto);
        Task<ApiResponse<bool>> DeleteCommentAsync(Guid commentId, Guid userId);
    }

    public interface IMediaService
    {
        Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder = "posts");
        Task<(string Url, string PublicId, string Duration, List<double> Waveform)> UploadAudioAsync(IFormFile file, string folder = "posts/audio");
        Task<(string Url, string PublicId, string? ThumbnailUrl, string? ThumbnailPublicId)> UploadVideoAsync(IFormFile file, string folder = "stories/video");
        Task<bool> DeleteImageAsync(string publicId);
        Task<bool> DeleteAudioAsync(string publicId);
        Task<bool> DeleteVideoAsync(string publicId);
    }

    public interface IStoryService
    {
        Task<ApiResponse<List<StoryFeedItemDto>>> GetStoryFeedAsync(Guid currentUserId);
        Task<ApiResponse<List<StoryDto>>> GetUserStoriesAsync(Guid userId, Guid? currentUserId);
        Task<ApiResponse<StoryDto>> GetStoryByIdAsync(Guid storyId, Guid? currentUserId);
        Task<ApiResponse<StoryDto>> CreateImageStoryAsync(Guid userId, IFormFile file);
        Task<ApiResponse<StoryDto>> CreateVideoStoryAsync(Guid userId, IFormFile file);
        Task<ApiResponse<bool>> DeleteStoryAsync(Guid storyId, Guid currentUserId);
        Task<ApiResponse<bool>> ViewStoryAsync(Guid storyId, Guid viewerId);
        Task<ApiResponse<List<StoryViewerDto>>> GetStoryViewersAsync(Guid storyId, Guid currentUserId);
    }

    public interface IUserProfileHttpClient
    {
        Task<UserProfileDto?> GetUserProfileAsync(Guid userId);
        Task<List<UserProfileDto>> GetUserProfilesAsync(List<Guid> userIds);
        Task<List<Guid>> GetFriendIdsAsync(Guid userId);
        Task<bool> UpdatePostsCountAsync(Guid userId, int count);
    }

    public interface IMessagePublisher
    {
        Task PublishPostCreatedAsync(Guid postId, Guid userId, string content);
        Task PublishPostDeletedAsync(Guid postId, Guid userId);
        Task PublishCommentCreatedAsync(Guid commentId, Guid postId, Guid userId, string content);
        Task PublishPostLikedAsync(Guid postId, Guid userId);
    }
}