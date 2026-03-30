using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Domain.Interfaces;
using System.Security.Claims;

namespace Notification.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly IUnitOfWork          _uow;

        public NotificationController(
            INotificationService notificationService,
            IUnitOfWork          uow)
        {
            _notificationService = notificationService;
            _uow                 = uow;
        }

        /// <summary>Get paginated notifications for the current user.</summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int page     = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService.GetNotificationsAsync(userId, page, pageSize);
            return Ok(ApiResponse<PaginatedResponse<NotificationDto>>.Ok(result));
        }

        /// <summary>Get unread notification count.</summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var count  = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(ApiResponse<int>.Ok(count));
        }

        /// <summary>Mark a single notification as read.</summary>
        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAsReadAsync(id, userId);
            return Ok(ApiResponse<object>.Ok(null!, "Notification marked as read"));
        }

        /// <summary>Mark all notifications as read.</summary>
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(ApiResponse<object>.Ok(null!, "All notifications marked as read"));
        }

        // ── Device Token ────────────────────────────────────────────────────────

        /// <summary>Register an FCM device token for push notifications.</summary>
        [HttpPost("device-token")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenDto dto)
        {
            var userId = GetCurrentUserId();

            // Upsert: if token already exists for this user, skip; otherwise add
            var existing = await _uow.DeviceTokens.GetByUserAndTokenAsync(userId, dto.Token);
            if (existing == null)
            {
                var token = DeviceToken.Create(userId, dto.Token, dto.Platform);
                await _uow.DeviceTokens.AddAsync(token);
                await _uow.SaveChangesAsync();
            }

            return Ok(ApiResponse<object>.Ok(null!, "Device token registered"));
        }

        /// <summary>Remove an FCM device token (e.g. on logout).</summary>
        [HttpDelete("device-token")]
        public async Task<IActionResult> RemoveDeviceToken([FromBody] RegisterDeviceTokenDto dto)
        {
            await _uow.DeviceTokens.DeleteByTokenAsync(dto.Token);
            await _uow.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(null!, "Device token removed"));
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }
}
