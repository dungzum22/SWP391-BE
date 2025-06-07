namespace PlatformFlower.Services.Common.Configuration
{
    public interface IJwtConfiguration
    {
        string SecretKey { get; }
        string Issuer { get; }
        string Audience { get; }
        int ExpirationMinutes { get; }
    }
}
