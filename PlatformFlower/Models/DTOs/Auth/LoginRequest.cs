namespace PlatformFlower.Models.DTOs.Auth
{
    /// <summary>
    /// User login request DTO
    /// Validation is handled by AuthValidation class
    /// </summary>
    public class LoginRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
