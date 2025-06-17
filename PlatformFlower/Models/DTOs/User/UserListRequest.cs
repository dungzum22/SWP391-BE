using System.ComponentModel.DataAnnotations;

namespace PlatformFlower.Models.DTOs.User
{
    public class UserListRequest
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public UserInfoManagement? UserInfo { get; set; }
    }

    public class UserInfoManagement
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Avatar { get; set; }
    }

    public class UserDetailResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public UserInfoManagement? UserInfo { get; set; }
        public SellerInfo? SellerInfo { get; set; }
    }

    public class SellerInfo
    {
        public int SellerId { get; set; }
        public string ShopName { get; set; } = null!;
        public string AddressSeller { get; set; } = null!;
        public string Role { get; set; } = null!;
        public int? TotalProduct { get; set; }
        public DateTime? CreatedAt { get; set; }
    }



    public class UserStatusUpdate
    {
        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 500 characters")]
        public string Reason { get; set; } = null!;
    }
}
