using PlatformFlower.Models.DTOs;

namespace PlatformFlower.Services.User.Profile
{
    public interface IProfileService
    {
        Task<UserResponseDto?> GetUserByIdAsync(int userId);
        Task<UserResponseDto?> GetUserByUsernameAsync(string username);
        Task<UserResponseDto> UpdateUserInfoAsync(int userId, UpdateUserInfoDto updateDto);
    }
}
