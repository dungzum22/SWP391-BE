namespace PlatformFlower.Services.Common.Configuration
{
    public interface IAwsConfiguration
    {
        string BucketName { get; }
        string Region { get; }
        string AccessKey { get; }
        string SecretKey { get; }
    }
}
