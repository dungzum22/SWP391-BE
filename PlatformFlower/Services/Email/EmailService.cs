using System.Net;
using System.Net.Mail;

namespace PlatformFlower.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IEmailConfiguration _emailConfig;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IEmailConfiguration emailConfig, ILogger<EmailService> logger)
        {
            _emailConfig = emailConfig;
            _logger = logger;
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string username)
        {
            try
            {
                var subject = "Chào mừng bạn đến với PlatformFlower!";
                var body = GenerateWelcomeEmailBody(username);

                await SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation($"Welcome email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send welcome email to {toEmail}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string username)
        {
            try
            {
                var subject = "Đặt lại mật khẩu - PlatformFlower";
                var body = GeneratePasswordResetEmailBody(resetToken, username);

                await SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation($"Password reset email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send password reset email to {toEmail}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                _logger.LogInformation($"Attempting to send email to {toEmail} with subject: {subject}");

                using var smtpClient = new SmtpClient(_emailConfig.SmtpServer, _emailConfig.Port);
                smtpClient.EnableSsl = true;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(_emailConfig.SenderEmail, _emailConfig.SenderPassword);

                using var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(_emailConfig.SenderEmail, "PlatformFlower");
                mailMessage.To.Add(toEmail);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {toEmail}: {ex.Message}", ex);
                throw;
            }
        }

        private string GenerateWelcomeEmailBody(string username)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; color: #666; }}
        .button {{ display: inline-block; padding: 10px 20px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🌸 Chào mừng đến với PlatformFlower! 🌸</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {username}!</h2>
            <p>Cảm ơn bạn đã đăng ký tài khoản tại <strong>PlatformFlower</strong> - nền tảng mua bán hoa trực tuyến hàng đầu!</p>

            <p>Tài khoản của bạn đã được tạo thành công. Bây giờ bạn có thể:</p>
            <ul>
                <li>🛒 Mua sắm các loại hoa tươi đẹp</li>
                <li>🌺 Khám phá bộ sưu tập hoa đa dạng</li>
                <li>💝 Tặng hoa cho người thân yêu</li>
                <li>⭐ Tích điểm và nhận ưu đãi đặc biệt</li>
            </ul>

            <p>Chúng tôi rất vui mừng được phục vụ bạn!</p>

            <p style='text-align: center; margin: 30px 0;'>
                <a href='#' class='button'>Bắt đầu mua sắm ngay!</a>
            </p>
        </div>
        <div class='footer'>
            <p>Trân trọng,<br><strong>Đội ngũ PlatformFlower</strong></p>
            <p><small>Email này được gửi tự động, vui lòng không trả lời.</small></p>
        </div>
    </div>
</body>
</html>";
        }

        private string GeneratePasswordResetEmailBody(string resetToken, string username)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF6B6B; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; color: #666; }}
        .token-box {{ background-color: #fff; border: 2px dashed #FF6B6B; padding: 15px; margin: 20px 0; text-align: center; font-family: monospace; font-size: 18px; font-weight: bold; color: #FF6B6B; }}
        .warning {{ background-color: #FFF3CD; border: 1px solid #FFEAA7; padding: 10px; margin: 15px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Đặt lại mật khẩu - PlatformFlower</h1>
        </div>
        <div class='content'>
            <h2>Xin chào {username}!</h2>
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn tại <strong>PlatformFlower</strong>.</p>

            <p>Để đặt lại mật khẩu, vui lòng sử dụng mã xác thực dưới đây:</p>

            <div class='token-box'>
                {resetToken}
            </div>

            <div class='warning'>
                <strong>⚠️ Lưu ý quan trọng:</strong>
                <ul>
                    <li>Mã xác thực này chỉ có hiệu lực trong <strong>15 phút</strong></li>
                    <li>Chỉ sử dụng mã này nếu bạn đã yêu cầu đặt lại mật khẩu</li>
                    <li>Không chia sẻ mã này với bất kỳ ai</li>
                </ul>
            </div>

            <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này. Tài khoản của bạn vẫn an toàn.</p>
        </div>
        <div class='footer'>
            <p>Trân trọng,<br><strong>Đội ngũ PlatformFlower</strong></p>
            <p><small>Email này được gửi tự động, vui lòng không trả lời.</small></p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
