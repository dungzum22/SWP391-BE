namespace PlatformFlower.Services.Common.Configuration
{
    public class AwsConfiguration : IAwsConfiguration
    {
        private readonly IConfiguration _configuration;

        public AwsConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string BucketName => _configuration["AWS:BucketName"] ?? "flower-shopz";
        public string Region => _configuration["AWS:Region"] ?? "ap-southeast-2";
        public string AccessKey => _configuration["AWS:AccessKey"] ?? "";
        public string SecretKey => _configuration["AWS:SecretKey"] ?? "";
    }
}
