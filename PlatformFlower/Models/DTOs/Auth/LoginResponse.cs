namespace PlatformFlower.Models.DTOs.Auth
{
    public class LoginResponse
    {
        public User.UserResponse User { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAt { get; set; }
        public int ExpiresInMinutes { get; set; }
    }

    public class RegisterResponse
    {
        public User.UserResponse User { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAt { get; set; }
        public int ExpiresInMinutes { get; set; }
    }
}
