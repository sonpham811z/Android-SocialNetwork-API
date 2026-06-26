using System;
using System.ComponentModel.DataAnnotations;

namespace Identity.Application.DTOs
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
        [Required]
        public string FirstName { get; set; }
        
        [Required]
        public string LastName { get; set; }
        
        [Required]
        public DateTime DateOfBirth { get; set; }
        
        public string Gender { get; set; }
    }

    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        public string Password { get; set; }
    }

    public class GoogleAuthDto
    {
        [Required]
        public string IdToken { get; set; }
    }

    public class RefreshTokenDto
    {
        [Required]
        public string RefreshToken { get; set; }
    }

    public class LogoutDto
    {
        public string RefreshToken { get; set; }
    }

    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; }
        
        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; }
    }

    public class VerifyEmailDto
    {
        [Required]
        public string Token { get; set; }
    }

    public class ResendVerificationDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
    
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword {  get; set; }

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; }
    }
    public class AuthResponseDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; }
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public bool FirstLogin { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ApiResponse<T> // T là generics ( kiểu chung/ biến đạu diện/ Type) 
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public string[] Errors {get; set; }

        public static ApiResponse<T> SuccessResponse(T data, string message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResponse(string message, string[] errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }
}