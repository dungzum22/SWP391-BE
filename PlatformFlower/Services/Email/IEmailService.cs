namespace PlatformFlower.Services.Email
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string toEmail, string username);

        Task SendEmailAsync(string toEmail, string subject, string body);

        Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string username);
    }
}
