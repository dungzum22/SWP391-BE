
using PlatformFlower.Models.DTOs.User;

namespace PlatformFlower.Services.Admin.UserManagement
{
    public interface IUserManagementService
    {
        Task<List<UserListRequest>> GetAllUsersAsync();
        Task<UserDetailResponse?> GetUserByIdAsync(int userId);
        Task<UserDetailResponse> ToggleUserStatusAsync(int userId, string reason);
    }
}
