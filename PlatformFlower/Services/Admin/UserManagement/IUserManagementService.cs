using PlatformFlower.Models.DTOs;

namespace PlatformFlower.Services.Admin.UserManagement
{
    public interface IUserManagementService
    {
        Task<PaginatedUsersResponseDto> GetUsersAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? userType = null, bool? isActive = null);
        Task<List<UserListResponseDto>> GetAllUsersAsync();
        Task<UserDetailResponseDto?> GetUserByIdAsync(int userId);
        Task<UserDetailResponseDto> ToggleUserStatusAsync(int userId, string reason);
    }
}
