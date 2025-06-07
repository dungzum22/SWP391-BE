namespace PlatformFlower.Models.DTOs
{
    public class AuthResponseDto
    {
        public UserResponseDto User { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAt { get; set; }
        public int ExpiresInMinutes { get; set; }
    }
}
