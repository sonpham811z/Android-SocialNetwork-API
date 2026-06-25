using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using User.Application.DTOs;
using User.Application.Interfaces;

namespace User.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _profileService;
        private readonly IUserSettingsService _settingsService;

        public UserProfileController(IUserProfileService profileService, IUserSettingsService settingsService)
        {
            _profileService = profileService;
            _settingsService = settingsService;
        }

        // GET: api/userprofile/{id}
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfileById(Guid id)
        {
            var result = await _profileService.GetProfileByIdAsync(id);
            
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/userprofile/userid/{userId}
        [HttpGet("userid/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfileByUserId(Guid userId)
        {
            var result = await _profileService.GetProfileByUserIdAsync(userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/userprofile/username/{username}
        [HttpGet("username/{username}")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfileByUsername(string username)
        {
            var result = await _profileService.GetProfileByUsernameAsync(username);
            
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/userprofile/me
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var result = await _profileService.GetProfileByUserIdAsync(userId);
            
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // POST: api/userprofile
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateProfile([FromBody] CreateUserProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _profileService.CreateProfileAsync(dto);
            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(
                nameof(GetProfileById), 
                new { id = result.Data.Id }, 
                result
            );
        }

        // PUT: api/userprofile
        [Authorize]
        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _profileService.UpdateProfileAsync(userId, dto);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // DELETE: api/userprofile
        [Authorize]
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProfile()
        {
            var userId = GetCurrentUserId();
            var result = await _profileService.DeleteProfileAsync(userId);
            
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/userprofile/search?q=john&page=1&pageSize=20
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<SearchResultDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchUsers(
            [FromQuery] string q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(ApiResponse<object>.ErrorResponse("Search query is required"));

            var result = await _profileService.SearchUsersAsync(q, page, pageSize);
            return Ok(result);
        }

        // POST: api/userprofile/upload-profile-picture
        [Authorize]
        [HttpPost("upload-profile-picture")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadProfilePicture([FromForm] IFormFile image)
        {
            if (image == null)
                return BadRequest(ApiResponse<string>.ErrorResponse("Image file is required"));

            var userId = GetCurrentUserId();
            var result = await _profileService.UploadProfilePictureAsync(userId, image);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // POST: api/userprofile/upload-cover-photo
        [Authorize]
        [HttpPost("upload-cover-photo")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadCoverPhoto([FromForm] IFormFile image)
        {
            if (image == null)
                return BadRequest(ApiResponse<string>.ErrorResponse("Image file is required"));

            var userId = GetCurrentUserId();
            var result = await _profileService.UploadCoverPhotoAsync(userId, image);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // Dùng HttpPost cho Batch API là best practice vì list ID truyền lên có thể rất dài,
        // nếu dùng HttpGet truyền qua URL sẽ bị lỗi giới hạn độ dài URL (URL too long).
        [AllowAnonymous]
        [HttpPost("batch")]
        [ProducesResponseType(typeof(ApiResponse<List<UserProfileDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProfilesBatch([FromBody] List<Guid> userIds)
        {
            var result = await _profileService.GetProfilesBatchAsync(userIds);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // DELETE: api/userprofile/profile-picture
        [Authorize]
        [HttpDelete("profile-picture")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteProfilePicture()
        {
            var userId = GetCurrentUserId();
            var result = await _profileService.DeleteProfilePictureAsync(userId);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // DELETE: api/userprofile/cover-photo
        [Authorize]
        [HttpDelete("cover-photo")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteCoverPhoto()
        {
            var userId = GetCurrentUserId();
            var result = await _profileService.DeleteCoverPhotoAsync(userId);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/userprofile/settings
        [Authorize]
        [HttpGet("settings")]
        [ProducesResponseType(typeof(ApiResponse<UserSettingsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSettings()
        {
            var userId = GetCurrentUserId();
            var result = await _settingsService.GetSettingsAsync(userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // GET: api/userprofile/settings/{userId}
        // Internal service-to-service endpoint (e.g. Notification service reads a recipient's
        // notification preferences before deciding whether to deliver a notification).
        [AllowAnonymous]
        [HttpGet("settings/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserSettingsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSettingsByUserId(Guid userId)
        {
            var result = await _settingsService.GetSettingsAsync(userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // PUT: api/userprofile/settings
        [Authorize]
        [HttpPut("settings")]
        [ProducesResponseType(typeof(ApiResponse<UserSettingsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _settingsService.UpdateSettingsAsync(userId, dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        private Guid GetCurrentUserId()
        {
           if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("User is not logged in."); 
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("Invalid User ID in Token.");
        }
    }
}