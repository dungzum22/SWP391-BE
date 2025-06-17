
using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Auth;
using System.Text.RegularExpressions;

namespace PlatformFlower.Services.User.Auth
{
    public static class AuthValidation
    {
        public static async Task ValidateRegistrationAsync(RegisterRequest request, FlowershopContext context)
        {
            ValidateRegistrationFields(request);
            await ValidateUsernameUniqueness(request.Username, context);
            await ValidateEmailUniqueness(request.Email, context);
        }

        private static void ValidateRegistrationFields(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                throw new ArgumentException("Username is required");
            }

            if (request.Username.Length < 3 || request.Username.Length > 255)
            {
                throw new ArgumentException("Username must be between 3 and 255 characters");
            }

            if (!IsValidUsernameFormat(request.Username))
            {
                throw new ArgumentException("Username can only contain letters, numbers, and underscores");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required");
            }

            if (request.Password.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters");
            }

            if (request.Password.Length > 255)
            {
                throw new ArgumentException("Password must not exceed 255 characters");
            }

            if (!IsValidPasswordComplexity(request.Password))
            {
                throw new ArgumentException("Password must contain at least one uppercase letter, one lowercase letter, and one number");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required");
            }

            if (request.Email.Length > 255)
            {
                throw new ArgumentException("Email must not exceed 255 characters");
            }

            if (!IsValidEmailFormat(request.Email))
            {
                throw new ArgumentException("Invalid email format");
            }
        }

        public static void ValidateLogin(LoginRequest request)
        {
            ValidateLoginFields(request);
        }

        private static void ValidateLoginFields(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                throw new ArgumentException("Username is required");
            }

            if (request.Username.Length > 255)
            {
                throw new ArgumentException("Username must not exceed 255 characters");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required");
            }

            if (request.Password.Length > 255)
            {
                throw new ArgumentException("Password must not exceed 255 characters");
            }
        }

        public static void ValidateResetPassword(ResetPasswordRequest request)
        {
            ValidateResetPasswordFields(request);
        }

        private static void ValidateResetPasswordFields(ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                throw new ArgumentException("Reset token is required");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                throw new ArgumentException("New password is required");
            }

            if (request.NewPassword.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters long");
            }

            if (!IsValidPasswordComplexity(request.NewPassword))
            {
                throw new ArgumentException("Password must contain at least one uppercase letter, one lowercase letter, and one number");
            }

            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                throw new ArgumentException("Confirm password is required");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new ArgumentException("Password and confirm password do not match");
            }
        }

        public static void ValidateForgotPassword(ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required");
            }

            if (!IsValidEmailFormat(request.Email))
            {
                throw new ArgumentException("Invalid email format");
            }
        }

        private static async Task ValidateUsernameUniqueness(string username, FlowershopContext context)
        {
            if (await context.Users.AnyAsync(u => u.Username == username))
            {
                throw new InvalidOperationException("Username already exists");
            }
        }

        private static async Task ValidateEmailUniqueness(string email, FlowershopContext context)
        {
            if (await context.Users.AnyAsync(u => u.Email == email))
            {
                throw new InvalidOperationException("Email already exists");
            }
        }

        private static bool IsValidEmailFormat(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            return emailRegex.IsMatch(email);
        }

        private static bool IsValidUsernameFormat(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            var usernameRegex = new Regex(@"^[a-zA-Z0-9_]+$");
            return usernameRegex.IsMatch(username);
        }

        private static bool IsValidPasswordComplexity(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);

            return hasUpper && hasLower && hasDigit;
        }
    }
}
