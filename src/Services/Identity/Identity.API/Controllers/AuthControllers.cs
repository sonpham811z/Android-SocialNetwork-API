using System.Threading.Tasks;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        private string GetIpAddress()
        {
            if(Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                return Request.Headers["X-Forwarded-For"]; //Return real user ip if use NginX 
            }

            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly= true,
                Expires = DateTime.UtcNow.AddDays(7),
                Secure = true,
                SameSite = SameSiteMode.None
            };

            Response.Cookies.Append("refreshToken", refreshToken,cookieOptions);
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(dto, GetIpAddress());

            if(!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(dto, GetIpAddress());

            if(!result.Success)
            {
                return BadRequest(result);
            }

            SetRefreshTokenCookie(result.Data.RefreshToken);
            return Ok(result);
        }

        [HttpPost("google")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.GoogleAuthAsync(dto, GetIpAddress());
            
            if (!result.Success)
                return BadRequest(result);

            SetRefreshTokenCookie(result.Data.RefreshToken);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            if(string.IsNullOrEmpty(refreshToken))
                return BadRequest(ApiResponse<AuthResponseDto>.ErrorResponse("Refresh token is required"));
            
            var result = await _authService.RefreshTokenAsync(refreshToken, GetIpAddress());

            SetRefreshTokenCookie(result.Data.RefreshToken);
            return Ok(result);
        }

        [HttpPost("verify-email")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.VerifyEmailAsync(dto.Token);

            if(!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("/verify-email")]
        [ApiExplorerSettings(IgnoreApi = true)] // Ẩn cái này khỏi Swagger cho gọn, vì nó dành cho Web Browser
        public async Task<IActionResult> VerifyEmailFromBrowser([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Content("Mã xác thực không hợp lệ.", "text/html; charset=utf-8");
            }

            // Tái sử dụng logic AuthService bro đã viết
            var result = await _authService.VerifyEmailAsync(token);

            string htmlContent;
            if (result.Success)
            {
                // Giao diện HTML đơn giản báo thành công và đá về App
                // Đổi 'socialapp' thành scheme tên app của bro nhé
                htmlContent = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <title>Xác thực thành công</title>
                        <style>
                            body { font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; text-align: center; padding: 50px; background-color: #f0f2f5; }
                            .container { background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); max-width: 400px; margin: auto; }
                            h2 { color: #0056b3; margin-bottom: 20px; }
                            p { color: #666; line-height: 1.6; }
                            .btn { display: inline-block; margin-top: 25px; padding: 12px 30px; background-color: #0056b3; color: white; text-decoration: none; border-radius: 8px; font-weight: bold; transition: opacity 0.3s; }
                            .btn:hover { opacity: 0.9; }
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h2>🎉 Xác nhận thành công!</h2>
                            <p>Tài khoản của bạn đã được xác minh.<br>Đang tự động chuyển hướng về ứng dụng...</p>
                            
                            <a href='socialapp://login' class='btn'>Mở lại ứng dụng</a>
                        </div>

                        <script>
                            // Tự động đá về app sau 2 giây bằng Deep Link
                            setTimeout(function() {
                                window.location.href = 'socialapp://login';
                            }, 2000);
                        </script>
                    </body>
                    </html>";
            }
            else
            {
                htmlContent = $@"
                    <!DOCTYPE html>
                    <html>
                    <head><meta charset='utf-8'><title>Xác thực thất bại</title></head>
                    <body style='text-align: center; padding: 50px; font-family: sans-serif; background-color: #f0f2f5;'>
                        <div style='background: white; padding: 40px; border-radius: 12px; max-width: 400px; margin: auto;'>
                            <h2 style='color: #dc3545;'>❌ Xác thực thất bại!</h2>
                            <p style='color: #666;'>{result.Message}</p>
                        </div>
                    </body>
                    </html>";
            }

            return Content(htmlContent, "text/html; charset=utf-8");
        }

        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Lấy UserId từ trong JWT Token (ClaimTypes.NameIdentifier thường lưu ID)
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(ApiResponse<bool>.ErrorResponse("Invalid or missing user token"));
            }

            // Gọi Service xử lý
            var result = await _authService.ChangePasswordAsync(userId, dto);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.ForgotPasswordAsync(dto);
            return Ok(result);
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ResetPasswordAsync(dto);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout([FromBody] LogoutDto dto)
        {
            if (dto != null && !string.IsNullOrEmpty(dto.RefreshToken))
            {
                // Gọi Service để xóa Token trong DB
                await _authService.LogoutAsync(dto.RefreshToken, GetIpAddress());
            }

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Logged out successfully"));
        }

        [HttpGet("/reset-password")]
        [ApiExplorerSettings(IgnoreApi = true)] // Ẩn khỏi Swagger
        public IActionResult ResetPasswordFromBrowser([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Content("Mã xác thực không hợp lệ hoặc đã hết hạn.", "text/html; charset=utf-8");
            }

            // Trả về HTML y hệt bên Verify Email để ép trình duyệt bật app Flutter lên
            // Lưu ý: Nhét luôn cái token vào đường dẫn socialapp://
            string htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Đặt lại mật khẩu</title>
                    <style>
                        body {{ font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; text-align: center; padding: 50px; background-color: #f0f2f5; }}
                        .container {{ background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); max-width: 400px; margin: auto; }}
                        h2 {{ color: #8B5CF6; margin-bottom: 20px; }}
                        p {{ color: #666; line-height: 1.6; }}
                        .btn {{ display: inline-block; margin-top: 25px; padding: 12px 30px; background-color: #8B5CF6; color: white; text-decoration: none; border-radius: 8px; font-weight: bold; transition: opacity 0.3s; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h2>Đang mở ứng dụng...</h2>
                        <p>Hệ thống đang chuyển hướng bạn về ứng dụng để đặt lại mật khẩu.</p>
                        <a href='socialapp://reset-password?token={token}' class='btn'>Mở ứng dụng thủ công</a>
                    </div>

                    <script>
                        // Tự động đá về app mang theo token
                        setTimeout(function() {{
                            window.location.href = 'socialapp://reset-password?token={token}';
                        }}, 1500);
                    </script>
                </body>
                </html>";

            return Content(htmlContent, "text/html; charset=utf-8");
        }

    }
}