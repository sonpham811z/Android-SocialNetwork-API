using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friend.Application.DTOs;
using Friend.Application.Interfaces;
using Friend.Domain.Entities;
using Friend.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Friend.Application.Services
{
    public class FriendRequestService : IFriendRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserProfileHttpClient _userClient;
        private readonly IMessagePublisher _publisher;
        private readonly ILogger<FriendRequestService> _logger;

        public FriendRequestService(
            IUnitOfWork unitOfWork,
            IUserProfileHttpClient userClient,
            IMessagePublisher publisher,
            ILogger<FriendRequestService> logger)
        {
            _unitOfWork = unitOfWork;
            _userClient = userClient;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<ApiResponse<FriendRequestDto>> SendRequestAsync(Guid senderId, SendFriendRequestDto dto)
        {
            try
            {
                var receiverId = dto.ReceiverId;

                if (senderId == receiverId)
                    return ApiResponse<FriendRequestDto>.ErrorResponse("Cannot send a friend request to yourself.");

                // Block guard — no interaction allowed in either direction
                if (await _unitOfWork.Blocks.IsBlockedAsync(senderId, receiverId))
                    return ApiResponse<FriendRequestDto>.ErrorResponse("Unable to send friend request.");

                // Already friends?
                if (await _unitOfWork.Friendships.AreFriendsAsync(senderId, receiverId))
                    return ApiResponse<FriendRequestDto>.ErrorResponse("You are already friends with this user.");

                // Duplicate pending request?
                var existing = await _unitOfWork.FriendRequests.GetPendingBetweenAsync(senderId, receiverId);
                if (existing != null)
                {
                    // If the OTHER person already sent a request, auto-accept
                    if (existing.SenderId == receiverId)
                    {
                        existing.Accept();
                        await _unitOfWork.SaveChangesAsync();

                        await EnsureFriendshipAsync(senderId, receiverId);
                        await _unitOfWork.SaveChangesAsync();

                        // await _publisher.PublishFriendRequestAcceptedAsync(existing.Id, existing.SenderId, existing.ReceiverId);

                        var autoDto = await MapRequestDtoAsync(existing);
                        return ApiResponse<FriendRequestDto>.SuccessResponse(autoDto, "Friend request automatically accepted.");
                    }

                    return ApiResponse<FriendRequestDto>.ErrorResponse("A pending friend request already exists.");
                }

                var request = FriendRequest.Create(senderId, receiverId);
                await _unitOfWork.FriendRequests.AddAsync(request);
                await _unitOfWork.SaveChangesAsync();

                // await _publisher.PublishFriendRequestSentAsync(request.Id, senderId, receiverId);

                var requestDto = await MapRequestDtoAsync(request);
                return ApiResponse<FriendRequestDto>.SuccessResponse(requestDto, "Friend request sent.");
            }
            catch (Exception ex)
            {
                return ApiResponse<FriendRequestDto>.ErrorResponse($"Error sending friend request: {ex.Message}");
            }
        }

        public async Task<ApiResponse<FriendRequestDto>> AcceptRequestAsync(Guid requestId, Guid currentUserId)
        {
            try
            {
                var request = await _unitOfWork.FriendRequests.GetByIdAsync(requestId);
                if (request == null || !request.IsPending)
                    return ApiResponse<FriendRequestDto>.ErrorResponse("Friend request not found or no longer pending.");

                if (!request.CanBeManagedBy(currentUserId))
                    return ApiResponse<FriendRequestDto>.ErrorResponse("You are not authorised to accept this request.");

                request.Accept();
                await _unitOfWork.SaveChangesAsync();

                await EnsureFriendshipAsync(request.SenderId, request.ReceiverId);
                await _unitOfWork.SaveChangesAsync();


                // await _publisher.PublishFriendRequestAcceptedAsync(request.Id, request.SenderId, request.ReceiverId);

                var dto = await MapRequestDtoAsync(request);
                return ApiResponse<FriendRequestDto>.SuccessResponse(dto, "Friend request accepted.");
            }
            catch (Exception ex)
            {
                return ApiResponse<FriendRequestDto>.ErrorResponse($"Error accepting friend request: {ex.Message}");
            }
        }

        public async Task<ApiResponse<FriendRequestDto>> DeclineRequestAsync(Guid requestId, Guid currentUserId)
        {
            try
            {
                var request = await _unitOfWork.FriendRequests.GetByIdAsync(requestId);
                if (request == null || !request.IsPending)
                    return ApiResponse<FriendRequestDto>.ErrorResponse("Friend request not found or no longer pending.");

                if (!request.CanBeManagedBy(currentUserId))
                    return ApiResponse<FriendRequestDto>.ErrorResponse("You are not authorised to decline this request.");

                request.Decline();
                await _unitOfWork.SaveChangesAsync();

                // await _publisher.PublishFriendRequestDeclinedAsync(request.Id, request.SenderId, request.ReceiverId);

                var dto = await MapRequestDtoAsync(request);
                return ApiResponse<FriendRequestDto>.SuccessResponse(dto, "Friend request declined.");
            }
            catch (Exception ex)
            {
                return ApiResponse<FriendRequestDto>.ErrorResponse($"Error declining friend request: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> CancelRequestAsync(Guid requestId, Guid currentUserId)
        {
            try
            {
                var request = await _unitOfWork.FriendRequests.GetByIdAsync(requestId);
                if (request == null || !request.IsPending)
                    return ApiResponse<bool>.ErrorResponse("Friend request not found or no longer pending.");

                if (!request.CanBeCancelledBy(currentUserId))
                    return ApiResponse<bool>.ErrorResponse("You are not authorised to cancel this request.");

                request.Cancel();
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResponse(true, "Friend request cancelled.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse($"Error cancelling friend request: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PaginatedResponse<FriendRequestDto>>> GetSentRequestsAsync(Guid userId, int page, int pageSize)
        {
            try
            {
                var requests = await _unitOfWork.FriendRequests.GetSentRequestsAsync(userId, page, pageSize);
                var dtos = new List<FriendRequestDto>();
                foreach (var r in requests)
                    dtos.Add(await MapRequestDtoAsync(r));

                return ApiResponse<PaginatedResponse<FriendRequestDto>>.SuccessResponse(new PaginatedResponse<FriendRequestDto>
                {
                    Items = dtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = dtos.Count
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<PaginatedResponse<FriendRequestDto>>.ErrorResponse($"Error retrieving sent requests: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PaginatedResponse<FriendRequestDto>>> GetReceivedRequestsAsync(Guid userId, int page, int pageSize)
        {
            try
            {
                var requests = await _unitOfWork.FriendRequests.GetReceivedRequestsAsync(userId, page, pageSize);
                _logger.LogDebug("Received friend requests: {@Requests}", requests);
                
                var dtos = new List<FriendRequestDto>();
                foreach (var r in requests)
                    dtos.Add(await MapRequestDtoAsync(r));

                return ApiResponse<PaginatedResponse<FriendRequestDto>>.SuccessResponse(new PaginatedResponse<FriendRequestDto>
                {
                    Items = dtos,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = dtos.Count
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<PaginatedResponse<FriendRequestDto>>.ErrorResponse($"Error retrieving received requests: {ex.Message}");
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private async Task EnsureFriendshipAsync(Guid userA, Guid userB)
        {
            var existingFriendship = await _unitOfWork.Friendships.GetByUsersIncludingDeletedAsync(userA, userB);

            if (existingFriendship == null)
            {
                var friendship = Friendship.Create(userA, userB);
                await _unitOfWork.Friendships.AddAsync(friendship);
                return;
            }

            if (existingFriendship.IsDeleted)
            {
                existingFriendship.Restore();
                await _unitOfWork.Friendships.UpdateAsync(existingFriendship);
            }
        }

        private async Task<FriendRequestDto> MapRequestDtoAsync(FriendRequest request)
        {
            var userIds = new List<Guid> { request.SenderId, request.ReceiverId };
            var users = await _userClient.GetUserProfilesAsync(userIds);

            var sender   = users.FirstOrDefault(u => u.UserId == request.SenderId)
                           ?? new UserProfileDto { UserId = request.SenderId, Name = "Unknown", UserName = "unknown" };
            var receiver = users.FirstOrDefault(u => u.UserId == request.ReceiverId)
                           ?? new UserProfileDto { UserId = request.ReceiverId, Name = "Unknown", UserName = "unknown" };

            return new FriendRequestDto
            {
                Id = request.Id,
                Sender = sender,
                Receiver = receiver,
                Status = request.Status.ToString(),
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };
        }

    }
}