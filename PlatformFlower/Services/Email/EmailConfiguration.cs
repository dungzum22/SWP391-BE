namespace PlatformFlower.Services.Email
{
    public class EmailConfiguration : IEmailConfiguration
    {
        private readonly IConfiguration _configuration;

        public EmailConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string SenderEmail => _configuration["EmailSettings:SenderEmail"] ?? "flowershopplatform@gmail.com";

        public string SenderPassword => _configuration["EmailSettings:SenderPassword"] ?? "";

        public string SmtpServer => _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";

        public int Port => int.Parse(_configuration["EmailSettings:Port"] ?? "587");
    }

    public interface IEmailConfiguration
    {
        string SenderEmail { get; }
        string SenderPassword { get; }
        string SmtpServer { get; }
        int Port { get; }
    }
}
