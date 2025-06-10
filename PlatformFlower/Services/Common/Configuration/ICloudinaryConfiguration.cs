namespace PlatformFlower.Services.Common.Configuration
{
    public interface ICloudinaryConfiguration
    {
        string CloudName { get; }
        string ApiKey { get; }
        string ApiSecret { get; }
    }
}
