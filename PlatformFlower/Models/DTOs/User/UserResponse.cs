namespace PlatformFlower.Models.DTOs.User
{
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
}
