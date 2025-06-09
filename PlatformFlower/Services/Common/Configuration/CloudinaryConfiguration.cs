using Microsoft.Extensions.Configuration;

namespace PlatformFlower.Services.Common.Configuration
{
    public class CloudinaryConfiguration : ICloudinaryConfiguration
    {
        private readonly IConfiguration _configuration;

        public CloudinaryConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CloudName => _configuration["Cloudinary:CloudName"] ?? string.Empty;
        public string ApiKey => _configuration["Cloudinary:ApiKey"] ?? string.Empty;
        public string ApiSecret => _configuration["Cloudinary:ApiSecret"] ?? string.Empty;
    }
}
