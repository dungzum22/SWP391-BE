namespace PlatformFlower.Services.Common.Configuration
{
    public class JwtConfiguration : IJwtConfiguration
    {
        private readonly IConfiguration _configuration;

        public JwtConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string SecretKey => _configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLongForPlatformFlower2024!";

        public string Issuer => _configuration["Jwt:Issuer"] ?? "PlatformFlower";

        public string Audience => _configuration["Jwt:Audience"] ?? "PlatformFlowerUsers";

        public int ExpirationMinutes => int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
    }
}
