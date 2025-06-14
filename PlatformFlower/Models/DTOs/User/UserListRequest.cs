using System.ComponentModel.DataAnnotations;

namespace PlatformFlower.Models.DTOs.User
{
    public class UserListRequestDto 
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public UserInfoManagementDto? UserInfo { get; set; }
    }

    public class UserInfoManagementDto
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Avatar { get; set; }
    }

    public class UserDetailResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public UserInfoManagementDto? UserInfo { get; set; }
        public SellerInfoDto? SellerInfo { get; set; }
    }

    public class SellerInfoDto
    {
        public int SellerId { get; set; }
        public string ShopName { get; set; } = null!;
        public string AddressSeller { get; set; } = null!;
        public string Role { get; set; } = null!;
        public int? TotalProduct { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class PaginatedUsersResponseDto
    {
        public List<UserListRequestDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class UserStatusUpdateDto
    {
        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 500 characters")]
        public string Reason { get; set; } = null!;
    }
}
