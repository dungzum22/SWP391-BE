
using PlatformFlower.Models.DTOs.User;

namespace PlatformFlower.Services.Admin.UserManagement
{
    public interface IUserManagementService
    {
        Task<PaginatedUsersResponse> GetUsersAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? userType = null, bool? isActive = null);
        Task<List<UserListRequest>> GetAllUsersAsync();
        Task<UserDetailResponse?> GetUserByIdAsync(int userId);
        Task<UserDetailResponse> ToggleUserStatusAsync(int userId, string reason);
    }
}
