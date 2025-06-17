namespace PlatformFlower.Models.DTOs.Auth
{
    /// <summary>
    /// User registration request DTO
    /// Validation is handled by AuthValidation class
    /// </summary>
    public class RegisterRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}
