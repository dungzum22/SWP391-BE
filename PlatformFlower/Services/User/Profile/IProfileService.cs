using PlatformFlower.Models.DTOs.User;

namespace PlatformFlower.Services.User.Profile
{
    public interface IProfileService
    {
        Task<UserResponse?> GetUserByIdAsync(int userId);
        Task<UserResponse?> GetUserByUsernameAsync(string username);
        Task<UserResponse> UpdateUserInfoAsync(int userId, UpdateUserRequest updateDto);
    }
}
