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
    public class StoryService : IStoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediaService _mediaService;
        private readonly IUserProfileHttpClient _userProfileClient;

        public StoryService(
            IUnitOfWork unitOfWork,
            IMediaService mediaService,
            IUserProfileHttpClient userProfileClient)
        {
            _unitOfWork = unitOfWork;
            _mediaService = mediaService;
            _userProfileClient = userProfileClient;
        }

        public async Task<ApiResponse<List<StoryFeedItemDto>>> GetStoryFeedAsync(Guid currentUserId)
        {
            try
            {
                var friendIds = await _userProfileClient.GetFriendIdsAsync(currentUserId);
                var userIds = new List<Guid>(friendIds) { currentUserId };

                var stories = await _unitOfWork.Stories.GetFeedStoriesAsync(userIds);
                var storyList = stories.ToList();

                if (!storyList.Any())
                    return ApiResponse<List<StoryFeedItemDto>>.SuccessResponse(new List<StoryFeedItemDto>());

                var allUserIds = storyList.Select(s => s.UserId).Distinct().ToList();
                var userProfiles = await _userProfileClient.GetUserProfilesAsync(allUserIds);

                
                var grouped = storyList
                    .GroupBy(s => s.UserId)
                    .Select(g =>
                    {
                        var profile = userProfiles.FirstOrDefault(u => u.UserId == g.Key)
                            ?? new UserProfileDto { Id = g.Key, Name = "Unknown", UserName = "unknown", FullName = "Unknown" };
                        // Console.WriteLine($"{u.Id} - {u.UserId}");
                        var storyDtos = g.OrderBy(s => s.CreatedAt)
                            .Select(s => MapToStoryDto(s, currentUserId, profile))
                            .ToList();

                        return new StoryFeedItemDto
                        {
                            User = profile,
                            Stories = storyDtos,
                            HasUnseenStories = storyDtos.Any(s => !s.IsViewedByCurrentUser)
                        };
                    })
                    // Own stories first, then sort by unseen
                    .OrderByDescending(item => item.User.Id == currentUserId)
                    .ThenByDescending(item => item.HasUnseenStories)
                    .ToList();

                return ApiResponse<List<StoryFeedItemDto>>.SuccessResponse(grouped);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<StoryFeedItemDto>>.ErrorResponse($"Error retrieving story feed: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<StoryDto>>> GetUserStoriesAsync(Guid userId, Guid? currentUserId)
        {
            try
            {
                var stories = await _unitOfWork.Stories.GetUserActiveStoriesAsync(userId);
                var profile = await _userProfileClient.GetUserProfileAsync(userId)
                    ?? new UserProfileDto { Id = userId, Name = "Unknown", UserName = "unknown", FullName = "Unknown" };

                var dtos = stories
                    .OrderBy(s => s.CreatedAt)
                    .Select(s => MapToStoryDto(s, currentUserId, profile))
                    .ToList();

                return ApiResponse<List<StoryDto>>.SuccessResponse(dtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<StoryDto>>.ErrorResponse($"Error retrieving user stories: {ex.Message}");
            }
        }

        public async Task<ApiResponse<StoryDto>> GetStoryByIdAsync(Guid storyId, Guid? currentUserId)
        {
            try
            {
                var story = await _unitOfWork.Stories.GetByIdAsync(storyId);
                if (story == null || story.IsDeleted || story.IsExpired())
                    return ApiResponse<StoryDto>.ErrorResponse("Story not found or expired");

                var profile = await _userProfileClient.GetUserProfileAsync(story.UserId)
                    ?? new UserProfileDto { Id = story.UserId, Name = "Unknown", UserName = "unknown", FullName = "Unknown" };

                return ApiResponse<StoryDto>.SuccessResponse(MapToStoryDto(story, currentUserId, profile));
            }
            catch (Exception ex)
            {
                return ApiResponse<StoryDto>.ErrorResponse($"Error retrieving story: {ex.Message}");
            }
        }

        public async Task<ApiResponse<StoryDto>> CreateImageStoryAsync(Guid userId, IFormFile file)
        {
            try
            {
                var (url, publicId) = await _mediaService.UploadImageAsync(file, "stories/image");
                var story = Story.CreateImageStory(userId, url, publicId);

                await _unitOfWork.Stories.AddAsync(story);
                await _unitOfWork.SaveChangesAsync();

                var profile = await _userProfileClient.GetUserProfileAsync(userId)
                    ?? new UserProfileDto { Id = userId, Name = "Unknown", UserName = "unknown", FullName = "Unknown" };

                return ApiResponse<StoryDto>.SuccessResponse(
                    MapToStoryDto(story, userId, profile), "Story created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<StoryDto>.ErrorResponse($"Error creating image story: {ex.Message}");
            }
        }

        public async Task<ApiResponse<StoryDto>> CreateVideoStoryAsync(Guid userId, IFormFile file)
        {
            try
            {
                var (url, publicId, thumbnailUrl, thumbnailPublicId) =
                    await _mediaService.UploadVideoAsync(file);

                var story = Story.CreateVideoStory(userId, url, publicId, thumbnailUrl, thumbnailPublicId);

                await _unitOfWork.Stories.AddAsync(story);
                await _unitOfWork.SaveChangesAsync();

                var profile = await _userProfileClient.GetUserProfileAsync(userId)
                    ?? new UserProfileDto { Id = userId, Name = "Unknown", UserName = "unknown", FullName = "Unknown" };

                return ApiResponse<StoryDto>.SuccessResponse(
                    MapToStoryDto(story, userId, profile), "Video story created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<StoryDto>.ErrorResponse($"Error creating video story: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteStoryAsync(Guid storyId, Guid currentUserId)
        {
            try
            {
                var story = await _unitOfWork.Stories.GetByIdAsync(storyId);
                if (story == null || story.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("Story not found");

                if (!story.CanBeDeletedBy(currentUserId))
                    return ApiResponse<bool>.ErrorResponse("You don't have permission to delete this story");

                if (!string.IsNullOrEmpty(story.MediaPublicId))
                {
                    if (story.MediaType == StoryMediaType.Image)
                        await _mediaService.DeleteImageAsync(story.MediaPublicId);
                    else
                        await _mediaService.DeleteVideoAsync(story.MediaPublicId);
                }

                if (!string.IsNullOrEmpty(story.ThumbnailPublicId))
                    await _mediaService.DeleteImageAsync(story.ThumbnailPublicId);

                story.SoftDelete();
                await _unitOfWork.Stories.UpdateAsync(story);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true, "Story deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error deleting story: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ViewStoryAsync(Guid storyId, Guid viewerId)
        {
            try
            {
                var story = await _unitOfWork.Stories.GetByIdAsync(storyId);
                if (story == null || story.IsDeleted || story.IsExpired())
                    return ApiResponse<bool>.ErrorResponse("Story not found or expired");

                var alreadyViewed = await _unitOfWork.Stories.HasUserViewedStoryAsync(storyId, viewerId);
                if (alreadyViewed)
                    return ApiResponse<bool>.SuccessResponse(true);

                var view = StoryView.Create(storyId, viewerId);
                await _unitOfWork.Stories.AddViewAsync(view);

                story.IncrementViewsCount();
                await _unitOfWork.Stories.UpdateAsync(story);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error recording story view: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<StoryViewerDto>>> GetStoryViewersAsync(Guid storyId, Guid currentUserId)
        {
            try
            {
                var story = await _unitOfWork.Stories.GetByIdWithViewsAsync(storyId);
                if (story == null || story.IsDeleted)
                    return ApiResponse<List<StoryViewerDto>>.ErrorResponse("Story not found");

                if (story.UserId != currentUserId)
                    return ApiResponse<List<StoryViewerDto>>.ErrorResponse("You can only see viewers of your own stories");

                var viewerIds = story.Views.Select(v => v.ViewerId).Distinct().ToList();
                var profiles = await _userProfileClient.GetUserProfilesAsync(viewerIds);

                var viewers = story.Views
                    .OrderByDescending(v => v.ViewedAt)
                    .Select(v =>
                    {
                        var profile = profiles.FirstOrDefault(p => p.Id == v.ViewerId)
                            ?? new UserProfileDto { Id = v.ViewerId, Name = "Unknown", UserName = "unknown", FullName = "Unknown" };
                        return new StoryViewerDto
                        {
                            ViewerId = v.ViewerId,
                            User = profile,
                            ViewedAt = v.ViewedAt
                        };
                    })
                    .ToList();

                return ApiResponse<List<StoryViewerDto>>.SuccessResponse(viewers);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<StoryViewerDto>>.ErrorResponse($"Error retrieving story viewers: {ex.Message}");
            }
        }

        private StoryDto MapToStoryDto(Story story, Guid? currentUserId, UserProfileDto profile)
        {
            bool isViewed = false;
            if (currentUserId.HasValue)
            {
                isViewed = story.Views.Any(v => v.ViewerId == currentUserId.Value);
            }

            return new StoryDto
            {
                Id = story.Id,
                UserId = story.UserId,
                User = profile,
                MediaUrl = story.MediaUrl,
                ThumbnailUrl = story.ThumbnailUrl,
                MediaType = story.MediaType.ToString(),
                ViewsCount = story.ViewsCount,
                IsViewedByCurrentUser = isViewed,
                IsOwner = currentUserId.HasValue && story.UserId == currentUserId.Value,
                CreatedAt = story.CreatedAt,
                ExpiresAt = story.ExpiresAt
            };
        }
    }
}
