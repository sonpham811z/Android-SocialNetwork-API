using brevo_csharp.Client;
using brevo_csharp.Model;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Identity.Application.Interfaces;
using brevo_csharp.Api;

namespace Identity.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly TransactionalEmailsApi _apiInstance;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;

            var apiKey = _configuration["Email:ApiKey"];

            if (!brevo_csharp.Client.Configuration.Default.ApiKey.ContainsKey("api-key"))
            {
                brevo_csharp.Client.Configuration.Default.ApiKey.Add("api-key", apiKey);
            }

            _apiInstance = new TransactionalEmailsApi();
        }

        private async System.Threading.Tasks.Task SendEmailViaBrevo(string toEmail, string subject, string htmlContent)
        {
            var senderName = _configuration["Email:FromName"];
            var senderEmail = _configuration["Email:FromEmail"];

            var sendEmail = new SendSmtpEmail(
                sender: new SendSmtpEmailSender(senderName, senderEmail),
                to: new List<SendSmtpEmailTo> {new SendSmtpEmailTo(toEmail)},
                subject: subject,
                htmlContent: htmlContent
            );

            try
            {
                var result = await _apiInstance.SendTransacEmailAsync(sendEmail);
                Console.WriteLine($"Email sent to {toEmail}. ID: {result.MessageId}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error sending email: {e.Message}");
                throw;
            }
        }

        public async System.Threading.Tasks.Task  SendVerificationEmailAsync(string email, string token)
        {
            var baseUrl = _configuration["App:BaseUrl"];
            var verificationLink = $"{baseUrl}/verify-email?token={token}";

            string appName = "Social Networking";
            string primaryColor = "#0056b3"; // Màu xanh dương chuyên nghiệp (Professional Blue)
            string subject = $"Xác thực tài khoản - {appName}";

            string htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Verify Email</title>
                <style>
                    /* Reset styles cho email client */
                    body {{ margin: 0; padding: 0; font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; background-color: #f0f2f5; color: #333333; }}
                    .email-wrapper {{ width: 100%; table-layout: fixed; background-color: #f0f2f5; padding-bottom: 40px; }}
                    .email-content {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.05); }}
                    
                    /* Header */
                    .header {{ background-color: {primaryColor}; padding: 25px; text-align: center; }}
                    .header h1 {{ color: #ffffff; margin: 0; font-size: 24px; font-weight: 600; letter-spacing: 0.5px; }}
                    
                    /* Body */
                    .body-content {{ padding: 40px 30px; text-align: left; line-height: 1.6; }}
                    .greeting {{ font-size: 18px; font-weight: bold; margin-bottom: 20px; color: #1a1a1a; }}
                    .text-paragraph {{ margin-bottom: 25px; font-size: 15px; color: #444444; }}
                    
                    /* Button */
                    .btn-container {{ text-align: center; margin: 35px 0; }}
                    .btn {{ display: inline-block; padding: 12px 30px; background-color: {primaryColor}; color: #ffffff !important; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 16px; transition: background-color 0.3s; }}
                    
                    /* Footer */
                    .footer {{ background-color: #fafafa; padding: 20px; text-align: center; font-size: 13px; color: #888888; border-top: 1px solid #eeeeee; }}
                    .footer p {{ margin: 5px 0; }}
                    
                    /* Link fallback */
                    .fallback-link {{ margin-top: 30px; padding-top: 20px; border-top: 1px dashed #e0e0e0; font-size: 13px; color: #666666; word-break: break-all; }}
                    .fallback-link a {{ color: {primaryColor}; text-decoration: none; }}
                </style>
            </head>
            <body>
                <div class='email-wrapper'>
                    <br> <div class='email-content'>
                        <div class='header'>
                            <h1>{appName}</h1>
                        </div>

                        <div class='body-content'>
                            <div class='greeting'>Xin chào,</div>
                            
                            <p class='text-paragraph'>
                                Cảm ơn bạn đã đăng ký tài khoản tại <strong>{appName}</strong>. <br>
                                Để bảo mật tài khoản và bắt đầu sử dụng dịch vụ, vui lòng xác nhận địa chỉ email của bạn bằng cách nhấn vào nút bên dưới.
                            </p>

                            <div class='btn-container'>
                                <a href='{verificationLink}' class='btn'>Xác thực Email ngay</a>
                            </div>

                            <p class='text-paragraph'>
                                Đường dẫn này sẽ hết hạn sau 24 giờ. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.
                            </p>

                            <div class='fallback-link'>
                                <p>Nếu nút bấm ở trên không hoạt động, bạn có thể sao chép và dán đường dẫn sau vào trình duyệt:</p>
                                <a href='{verificationLink}'>{verificationLink}</a>
                            </div>
                        </div>

                        <div class='footer'>
                            <p>&copy; {DateTime.Now.Year} {appName} Development Team.</p>
                            <p>Đây là email tự động, vui lòng không trả lời email này.</p>
                        </div>
                    </div>
                </div>
            </body>
            </html>";

            await SendEmailViaBrevo(email, subject, htmlContent);
        }

        public async System.Threading.Tasks.Task  SendPasswordResetEmailAsync(string email, string token)
        {
            var baseUrl = _configuration["App:BaseUrl"];
            var resetLink = $"{baseUrl}/reset-password?token={token}";

            string subject = "Reset Password";
            string htmlContent = $@"<p>Reset link: <a href='{resetLink}'>{resetLink}</a></p>";

            await SendEmailViaBrevo(email, subject, htmlContent);
        }
    }
}