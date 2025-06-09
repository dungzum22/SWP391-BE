using PlatformFlower.Models.DTOs;

namespace PlatformFlower.Services.User.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterUserAsync(RegisterUserDto registerDto);
        Task<AuthResponseDto> LoginUserAsync(LoginUserDto loginDto);
        Task<ForgotPasswordResponseDto> ForgotPasswordAsync(string email);
        Task<ForgotPasswordResponseDto> ResetPasswordAsync(ResetPasswordDto resetDto);
        Task<bool> ValidateResetTokenAsync(string token);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<bool> IsEmailExistsAsync(string email);
    }
}
