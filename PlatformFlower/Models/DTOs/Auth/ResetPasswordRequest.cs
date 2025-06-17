namespace PlatformFlower.Models.DTOs.Auth
{
    /// <summary>
    /// Password reset request DTO
    /// Validation is handled by AuthValidation class
    /// </summary>
    public class ResetPasswordRequest
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}
