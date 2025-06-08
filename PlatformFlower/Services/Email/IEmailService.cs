namespace PlatformFlower.Services.Email
{
    public interface IEmailService
    {
        /// <summary>
        /// Send welcome email to newly registered user
        /// </summary>
        /// <param name="toEmail">Recipient email address</param>
        /// <param name="username">Username of the registered user</param>
        /// <returns>Task representing the async operation</returns>
        Task SendWelcomeEmailAsync(string toEmail, string username);

        /// <summary>
        /// Send a generic email
        /// </summary>
        /// <param name="toEmail">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML)</param>
        /// <returns>Task representing the async operation</returns>
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
