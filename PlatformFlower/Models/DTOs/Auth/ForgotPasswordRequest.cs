namespace PlatformFlower.Models.DTOs.Auth
{
    /// <summary>
    /// Forgot password request DTO
    /// Validation is handled by AuthValidation class
    /// </summary>
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = null!;
    }
}
