using System.ComponentModel.DataAnnotations;

namespace PlatformFlower.Models.DTOs
{
    // Legacy DTOs for backward compatibility
    public class AuthResponseDto
    {
        public UserResponseDto User { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAt { get; set; }
        public int ExpiresInMinutes { get; set; }
    }

    public class LoginUserDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(255, ErrorMessage = "Username must not exceed 255 characters")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, ErrorMessage = "Password must not exceed 255 characters")]
        public string Password { get; set; } = null!;
    }

    public class RegisterUserDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(255, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 255 characters")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email must not exceed 255 characters")]
        public string Email { get; set; } = null!;
    }

    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;
    }

    public class ForgotPasswordResponseDto
    {
        public string Message { get; set; } = null!;
        public bool Success { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Reset token is required")]
        public string Token { get; set; } = null!;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("NewPassword", ErrorMessage = "Password and confirm password do not match")]
        public string ConfirmPassword { get; set; } = null!;
    }

    public class UserResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateTime? CreatedDate { get; set; }
        public string? Status { get; set; }
        
        // UserInfo properties
        public UserInfoDto? UserInfo { get; set; }
    }

    public class UserInfoDto
    {
        public int UserInfoId { get; set; }
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? Sex { get; set; }
        public bool? IsSeller { get; set; }
        public string? Avatar { get; set; }
        public int? Points { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class UpdateUserInfoDto
    {
        [StringLength(255, ErrorMessage = "Full name must not exceed 255 characters")]
        public string? FullName { get; set; }

        [StringLength(500, ErrorMessage = "Address must not exceed 500 characters")]
        public string? Address { get; set; }

        public DateOnly? BirthDate { get; set; }

        [RegularExpression("^(male|female|other)$", ErrorMessage = "Sex must be 'male', 'female', or 'other'")]
        public string? Sex { get; set; }

        public bool? IsSeller { get; set; }

        // Avatar will be uploaded separately via file upload
        public IFormFile? Avatar { get; set; }
    }

    public class UserListResponseDto
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
        public List<UserListResponseDto> Users { get; set; } = new();
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

    public class SellerResponseDto
    {
        public int SellerId { get; set; }
        public int UserId { get; set; }
        public string ShopName { get; set; } = null!;
        public string AddressSeller { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? TotalProduct { get; set; }
        public string Role { get; set; } = null!;
        public string? Introduction { get; set; }
        
        // Include user information for convenience
        public UserResponseDto? User { get; set; }
    }

    public class UpdateSellerDto
    {
        [Required(ErrorMessage = "Shop name is required")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Shop name must be between 2 and 255 characters")]
        public string ShopName { get; set; } = null!;

        [Required(ErrorMessage = "Seller address is required")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Seller address must be between 5 and 255 characters")]
        public string AddressSeller { get; set; } = null!;

        [Required(ErrorMessage = "Role is required")]
        [RegularExpression("^(individual|enterprise)$", ErrorMessage = "Role must be 'individual' or 'enterprise'")]
        public string Role { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Introduction must not exceed 1000 characters")]
        public string? Introduction { get; set; }
    }

    public class CategoryManageRequestDto
    {
        /// <summary>
        /// Category ID - 0 or null for CREATE, > 0 for UPDATE/DELETE
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Category name - required for CREATE and UPDATE
        /// </summary>
        [StringLength(255, ErrorMessage = "Category name cannot exceed 255 characters")]
        public string? CategoryName { get; set; }

        /// <summary>
        /// Status - 'active' or 'inactive'
        /// </summary>
        [StringLength(20)]
        public string? Status { get; set; }

        /// <summary>
        /// Set to true for DELETE operation (soft delete)
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }

    public class CategoryResponseDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int FlowerCount { get; set; } // Number of flowers in this category
    }
}
