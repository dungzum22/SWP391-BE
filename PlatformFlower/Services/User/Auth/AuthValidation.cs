
using PlatformFlower.Services.Common.Validation;
using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Auth;

namespace PlatformFlower.Services.User.Auth
{
    public static class AuthValidation
    {
        public static async Task ValidateRegistrationAsync(RegisterRequest registerDto, FlowershopContext context, IValidationService validationService)
        {
            if (await context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                throw new InvalidOperationException("Username already exists");
            }

            if (await context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            if (!validationService.IsValidEmail(registerDto.Email))
            {
                throw new ArgumentException("Invalid email format");
            }

            if (!validationService.IsValidPassword(registerDto.Password))
            {
                throw new ArgumentException("Password does not meet security requirements");
            }
        }

        public static void ValidateLogin(LoginRequest loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Username))
            {
                throw new ArgumentException("Username is required");
            }

            if (string.IsNullOrWhiteSpace(loginDto.Password))
            {
                throw new ArgumentException("Password is required");
            }
        }

        public static void ValidateResetPassword(ResetPasswordRequest resetDto)
        {
            if (string.IsNullOrWhiteSpace(resetDto.Token))
            {
                throw new ArgumentException("Reset token is required");
            }

            if (string.IsNullOrWhiteSpace(resetDto.NewPassword))
            {
                throw new ArgumentException("New password is required");
            }

            if (resetDto.NewPassword != resetDto.ConfirmPassword)
            {
                throw new ArgumentException("Password and confirm password do not match");
            }
        }
    }
}
