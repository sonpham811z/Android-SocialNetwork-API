using System;
using System.Threading.Tasks;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces;
using Identity.Domain.Events;

using Microsoft.Extensions.Configuration;

namespace Identity.Application.Services
{
    public class AuthService : IAuthService
    {
        //=== OOP Skill
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IEmailVerificationRepository _emailVerificationRepository;
        private readonly IPasswordResetRepository _passwordResetRepository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IEventPublisher _eventPublisher;
        private readonly IUserServiceClient _userServiceClient;

        // Constructor
        public AuthService (
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IEmailVerificationRepository emailVerificationRepository,
            IPasswordResetRepository passwordResetRepository,
            IJwtService jwtService,
            IPasswordHasher passwordHasher,
            IGoogleAuthService googleAuthService,
            IEmailService emailService,
            IConfiguration configuration,
            IEventPublisher eventPublisher,
            IUserServiceClient userServiceClient
        )
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _emailVerificationRepository = emailVerificationRepository;
            _passwordResetRepository = passwordResetRepository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _googleAuthService = googleAuthService;
            _emailService = emailService;
            _configuration = configuration;
            _eventPublisher = eventPublisher;
            _userServiceClient = userServiceClient;
        }
        
        private async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userid, string ipAddress)
        {   
            var refreshToken = new RefreshToken
            {
                UserId = userid,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            await _refreshTokenRepository.CreateAsync(refreshToken);
            await _refreshTokenRepository.RemoveExpiredTokensAsync(userid);

            return refreshToken;
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                IsEmailConfirmed = user.IsEmailConfirmed,
                FirstLogin = user.FirstLogin,
                IsAdmin = user.IsAdmin,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<ApiResponse<UserDto>>RegisterAsync(RegisterDto dto, string ipAddress)
        {
            if(await _userRepository.EmailExistsAsync(dto.Email))
            {
                return ApiResponse<UserDto>.ErrorResponse("Email already exists");
            }

            //create user
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
            };

            user = await _userRepository.CreateAsync(user);

            //Pulish event create user
            await _eventPublisher.PublishAsync(new UserRegisteredEvent
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth
            });

            // Tạo profile trực tiếp (không qua RabbitMQ) để đảm bảo profile luôn tồn tại
            _ = _userServiceClient.EnsureProfileCreatedAsync(
                user.Id, user.Email, user.FirstName, user.LastName,
                user.DateOfBirth, user.Gender);


            // Send email verification
            var verificationToken = new EmailVerificationToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow
            };

            await _emailVerificationRepository.CreateAsync(verificationToken);
            //send
            _ = Task.Run(async () => 
            {
                try 
                {
                    await _emailService.SendVerificationEmailAsync(user.Email, verificationToken.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi gửi email ngầm: {ex.Message}");
                }
            });
            

            //Generate access, refreshtoken
            return ApiResponse<UserDto>.SuccessResponse(
                MapToUserDto(user),
                "Registration successful, Please check your email to verify account"
            );
        }

        public async Task<ApiResponse<AuthResponseDto>>LoginAsync(LoginDto dto, string ipAddress)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid email or password");
            }

            if (!user.IsEmailConfirmed)
                return ApiResponse<AuthResponseDto>.ErrorResponse("Please verify your email");

            if(!user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Account is deactived");
            }

            // Update last login bro
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress);

            return ApiResponse<AuthResponseDto>.SuccessResponse(
                new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    User = MapToUserDto(user)
                },
                "Login successful"
            );
        }

        public async Task<ApiResponse<AuthResponseDto>> GoogleAuthAsync(GoogleAuthDto dto, string ipAddress)
        {
            var googleUser = await _googleAuthService.ValidateGoogleTokenAsync(dto.IdToken);

            if(googleUser == null)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid Google Token");
            }

            //Check user exsits
            var user = await _userRepository.GetByGoogleIdAsync(googleUser.GoogleId);

            if(user == null)
            {
                user = await _userRepository.GetByEmailAsync(googleUser.Email);

                if(user == null)
                {
                    //Create new user
                    user = new User
                    {
                        Email = googleUser.Email,
                        GoogleId = googleUser.GoogleId,
                        FirstName = googleUser.FirstName,
                        LastName = googleUser.LastName,
                        IsEmailConfirmed = true
                    };
                    user = await _userRepository.CreateAsync(user);

                    //Publish event - register by google auth
                    await _eventPublisher.PublishAsync(new UserGoogleRegisteredEvent
                    {
                        UserId = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                    });

                    _ = _userServiceClient.EnsureProfileCreatedAsync(
                        user.Id, user.Email, user.FirstName, user.LastName,
                        dateOfBirth: null, gender: null);
                }
                else
                {
                    //Link google account
                    user.GoogleId = googleUser.GoogleId;
                    user.IsEmailConfirmed = true;
                    await _userRepository.UpdateAsync(user);
                }
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress);

            return ApiResponse<AuthResponseDto>.SuccessResponse(
                new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    User = MapToUserDto(user)
                },
                "Google authentication successful"
            );
        }

        public async Task<ApiResponse<AuthResponseDto>>RefreshTokenAsync(string token, string ipAddress)
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);

            if(refreshToken == null || !refreshToken.IsActive)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("Invalid or expired refresh token");
            }

            var user = await _userRepository.GetByIdAsync(refreshToken.UserId);

            if(user == null || !user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.ErrorResponse("User not found or inactive");
            }

            await _refreshTokenRepository.RevokeAsync(token, ipAddress);

            var accessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress);

            return ApiResponse<AuthResponseDto>.SuccessResponse(
                new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    User = MapToUserDto(user)
                }
            );
            
        }

        public async Task<ApiResponse<bool>> VerifyEmailAsync(string token)
        {
            var verificationToken = await _emailVerificationRepository.GetByTokenAsync(token);
            
            if (verificationToken == null || verificationToken.IsUsed || verificationToken.ExpiresAt < DateTime.UtcNow)
            {
                return ApiResponse<bool>.ErrorResponse("Invalid or expired verification token");
            }

            var user = await _userRepository.GetByIdAsync(verificationToken.UserId);
            user.IsEmailConfirmed = true;
            await _userRepository.UpdateAsync(user);

            await _emailVerificationRepository.MarkAsUsedAsync(verificationToken.Id);

            return ApiResponse<bool>.SuccessResponse(true, "Email verified successfully");
        }

        public async Task<ApiResponse<bool>> ResendVerificationEmailAsync(ResendVerificationDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            // Don't reveal whether the email exists
            if (user == null)
            {
                return ApiResponse<bool>.SuccessResponse(true, "If the email exists, a verification link has been sent");
            }

            if (user.IsEmailConfirmed)
            {
                return ApiResponse<bool>.ErrorResponse("Email is already verified");
            }

            // Issue a fresh verification token (old ones simply expire / stay unused)
            var verificationToken = new EmailVerificationToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow
            };

            await _emailVerificationRepository.CreateAsync(verificationToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendVerificationEmailAsync(user.Email, verificationToken.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi gửi email ngầm: {ex.Message}");
                }
            });

            return ApiResponse<bool>.SuccessResponse(true, "If the email exists, a verification link has been sent");
        }

        public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return ApiResponse<UserDto>.ErrorResponse("User not found");

            return ApiResponse<UserDto>.SuccessResponse(MapToUserDto(user));
        }

        public async Task<ApiResponse<bool>> CompleteIntroAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return ApiResponse<bool>.ErrorResponse("User not found");

            if (user.FirstLogin)
            {
                user.FirstLogin = false;
                await _userRepository.UpdateAsync(user);
            }

            return ApiResponse<bool>.SuccessResponse(true, "Intro completed");
        }

        public async Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            
            if (user == null)
            {
                // Don't reveal if email exists
                return ApiResponse<bool>.SuccessResponse(true, "If the email exists, a reset link has been sent");
            }

            var resetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow
            };
            await _passwordResetRepository.CreateAsync(resetToken);

            await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken.Token);

            return ApiResponse<bool>.SuccessResponse(true, "If the email exists, a reset link has been sent");
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if(user == null || !user.IsActive || !user.IsEmailConfirmed)
            {
                return ApiResponse<bool>.ErrorResponse("User invalid");
            }

            if (!_passwordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            {
                return ApiResponse<bool>.ErrorResponse("Incorrect current password");
            }

            // 3. (Tùy chọn) Check xem pass mới có trùng pass cũ không
            if (_passwordHasher.VerifyPassword(dto.NewPassword, user.PasswordHash))
            {
                return ApiResponse<bool>.ErrorResponse("New password must be different from current password");
            }

            // 4. Băm mật khẩu mới và lưu vào DB
            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            await _userRepository.UpdateAsync(user);

            // 5. Thu hồi toàn bộ Refresh Token cũ (bắt buộc các thiết bị khác phải login lại)
            // await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);

            return ApiResponse<bool>.SuccessResponse(true, "Password changed successfully");
            
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var resetToken = await _passwordResetRepository.GetByTokenAsync(dto.Token);
            
            if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
            {
                return ApiResponse<bool>.ErrorResponse("Invalid or expired reset token");
            }

            var user = await _userRepository.GetByIdAsync(resetToken.UserId);
            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
            await _userRepository.UpdateAsync(user);

            await _passwordResetRepository.MarkAsUsedAsync(resetToken.Id);

            // Revoke all refresh tokens
            await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);

            return ApiResponse<bool>.SuccessResponse(true, "Password reset successfully");
        }

        public async Task<ApiResponse<bool>> LogoutAsync(string token, string ipAddress)
        {
            await _refreshTokenRepository.RevokeAsync(token, ipAddress);
            return ApiResponse<bool>.SuccessResponse(true, "Logged out successfully");
        }


    }
}

