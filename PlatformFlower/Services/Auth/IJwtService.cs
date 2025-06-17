using PlatformFlower.Models.DTOs.User;

namespace PlatformFlower.Services.Auth
{
    public interface IJwtService
    {
        string GenerateToken(UserResponse user);

        bool ValidateToken(string token);

        int? GetUserIdFromToken(string token);

        string? GetUsernameFromToken(string token);
    }
}
