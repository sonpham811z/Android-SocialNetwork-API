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
    public class BoardService : IBoardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserProfileHttpClient _userProfileClient;

        public BoardService(IUnitOfWork unitOfWork, IUserProfileHttpClient userProfileClient)
        {
            _unitOfWork = unitOfWork;
            _userProfileClient = userProfileClient;
        }

        public async Task<ApiResponse<PaginatedResponse<BoardPostDto>>> GetPostsAsync(
            string? tag, string sort, int page, int pageSize, Guid? currentUserId)
        {
            try
            {
                BoardTag? boardTag = ParseTag(tag);
                bool sortByHot = sort?.ToLower() != "new";

                var posts = await _unitOfWork.Board.GetPostsAsync(boardTag, sortByHot, page, pageSize);
                var total = await _unitOfWork.Board.CountPostsAsync(boardTag);

                var dtos = new List<BoardPostDto>();
                foreach (var post in posts)
                {
                    var dto = await MapToDtoAsync(post, currentUserId);
                    dtos.Add(dto);
                }

                return ApiResponse<PaginatedResponse<BoardPostDto>>.SuccessResponse(
                    new PaginatedResponse<BoardPostDto>
                    {
                        Items = dtos,
                        TotalItems = total,
                        Page = page,
                        PageSize = pageSize,
                        TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                        HasNextPage = page * pageSize < total,
                        HasPreviousPage = page > 1
                    });
            }
            catch (Exception ex)
            {
                return ApiResponse<PaginatedResponse<BoardPostDto>>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<BoardPostDto>> CreatePostAsync(Guid userId, CreateBoardPostDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Content))
                    return ApiResponse<BoardPostDto>.ErrorResponse("Content is required");

                var tag = ParseTag(dto.Tag) ?? BoardTag.TamSu;
                var post = BoardPost.Create(userId, tag, dto.Content, dto.IsAnonymous);

                await _unitOfWork.Board.AddAsync(post);
                await _unitOfWork.SaveChangesAsync();

                var result = await MapToDtoAsync(post, userId);
                return ApiResponse<BoardPostDto>.SuccessResponse(result, "Post created");
            }
            catch (Exception ex)
            {
                return ApiResponse<BoardPostDto>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> VoteAsync(Guid postId, Guid userId, string voteType)
        {
            try
            {
                var post = await _unitOfWork.Board.GetByIdAsync(postId);
                if (post == null || post.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("Post not found");

                var newType = voteType.ToLower() == "up" ? VoteType.Up : VoteType.Down;
                var existing = await _unitOfWork.Board.GetVoteIncludingDeletedAsync(postId, userId);

                if (existing == null)
                {
                    await _unitOfWork.Board.AddVoteAsync(BoardVote.Create(postId, userId, newType));
                    ApplyVote(post, newType, +1);
                }
                else if (existing.IsDeleted)
                {
                    existing.Restore(newType);
                    await _unitOfWork.Board.UpdateVoteAsync(existing);
                    ApplyVote(post, newType, +1);
                }
                else if (existing.Type == newType)
                {
                    // Same vote → toggle off
                    existing.SoftDelete();
                    await _unitOfWork.Board.UpdateVoteAsync(existing);
                    ApplyVote(post, newType, -1);
                }
                else
                {
                    // Switch vote direction
                    ApplyVote(post, existing.Type, -1);
                    existing.ChangeType(newType);
                    await _unitOfWork.Board.UpdateVoteAsync(existing);
                    ApplyVote(post, newType, +1);
                }

                await _unitOfWork.Board.UpdateAsync(post);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteVoteAsync(Guid postId, Guid userId)
        {
            try
            {
                var post = await _unitOfWork.Board.GetByIdAsync(postId);
                if (post == null) return ApiResponse<bool>.ErrorResponse("Post not found");

                var vote = await _unitOfWork.Board.GetVoteAsync(postId, userId);
                if (vote == null) return ApiResponse<bool>.SuccessResponse(true);

                ApplyVote(post, vote.Type, -1);
                vote.SoftDelete();
                await _unitOfWork.Board.UpdateVoteAsync(vote);
                await _unitOfWork.Board.UpdateAsync(post);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeletePostAsync(Guid postId, Guid userId)
        {
            try
            {
                var post = await _unitOfWork.Board.GetByIdAsync(postId);
                if (post == null || post.IsDeleted)
                    return ApiResponse<bool>.ErrorResponse("Post not found");

                if (!post.CanBeDeletedBy(userId))
                    return ApiResponse<bool>.ErrorResponse("No permission to delete this post");

                post.SoftDelete();
                await _unitOfWork.Board.UpdateAsync(post);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true, "Post deleted");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error: {ex.Message}");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void ApplyVote(BoardPost post, VoteType type, int delta)
        {
            if (type == VoteType.Up)
            {
                if (delta > 0) post.IncrementUpvotes();
                else post.DecrementUpvotes();
            }
            else
            {
                if (delta > 0) post.IncrementDownvotes();
                else post.DecrementDownvotes();
            }
        }

        private async Task<BoardPostDto> MapToDtoAsync(BoardPost post, Guid? currentUserId)
        {
            string? authorName = null;
            string? authorAvatar = null;
            string? authorId = null;

            if (!post.IsAnonymous && post.AuthorId.HasValue)
            {
                var profile = await _userProfileClient.GetUserProfileAsync(post.AuthorId.Value);
                authorName = profile?.Name ?? "Unknown";
                authorAvatar = profile?.ProfilePictureUrl;
                authorId = post.AuthorId.Value.ToString();
            }

            string? currentVote = null;
            if (currentUserId.HasValue)
            {
                var vote = await _unitOfWork.Board.GetVoteAsync(post.Id, currentUserId.Value);
                currentVote = vote?.Type == VoteType.Up ? "up" : vote?.Type == VoteType.Down ? "down" : null;
            }

            return new BoardPostDto
            {
                Id = post.Id,
                Tag = TagToString(post.Tag),
                Content = post.Content,
                IsAnonymous = post.IsAnonymous,
                AuthorId = authorId,
                AuthorName = authorName,
                AuthorAvatar = authorAvatar,
                UpvotesCount = post.UpvotesCount,
                DownvotesCount = post.DownvotesCount,
                CommentsCount = post.CommentsCount,
                NetVotes = post.UpvotesCount - post.DownvotesCount,
                CurrentUserVote = currentVote,
                CreatedAt = post.CreatedAt,
                TimeAgo = FormatTimeAgo(post.CreatedAt)
            };
        }

        private static BoardTag? ParseTag(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return null;
            return tag.ToLower() switch
            {
                "hoibai" or "hỏibài"   => BoardTag.HoiBai,
                "timeline"             => BoardTag.Timeline,
                "timphong" or "tìmphòng" => BoardTag.TimPhong,
                "tamsu" or "tâmsự"    => BoardTag.TamSu,
                "saledo" or "saleđồ"  => BoardTag.SaleDo,
                _ => null
            };
        }

        private static string TagToString(BoardTag tag) => tag switch
        {
            BoardTag.HoiBai   => "hỏibài",
            BoardTag.Timeline => "timeline",
            BoardTag.TimPhong => "tìmphòng",
            BoardTag.TamSu    => "tâmsự",
            BoardTag.SaleDo   => "saleđồ",
            _ => "tâmsự"
        };

        private static string FormatTimeAgo(DateTime createdAt)
        {
            var diff = DateTime.UtcNow - createdAt;
            if (diff.TotalMinutes < 1)  return "Vừa xong";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
            if (diff.TotalHours < 24)   return $"{(int)diff.TotalHours} giờ trước";
            if (diff.TotalDays < 7)     return $"{(int)diff.TotalDays} ngày trước";
            return createdAt.ToString("dd/MM/yyyy");
        }
    }
}
