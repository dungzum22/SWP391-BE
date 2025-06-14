

using PlatformFlower.Models.DTOs.Auth;

namespace PlatformFlower.Services.User.Auth
{
    public interface IAuthService
    {
        Task<LoginResponse> RegisterUserAsync(RegisterRequest registerDto);
        Task<LoginResponse> LoginUserAsync(LoginRequest loginDto);
        Task<ForgotPasswordResponse> ForgotPasswordAsync(string email);
        Task<ForgotPasswordResponse> ResetPasswordAsync(ResetPasswordRequest resetDto);
        Task<bool> ValidateResetTokenAsync(string token);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<bool> IsEmailExistsAsync(string email);
    }
}
