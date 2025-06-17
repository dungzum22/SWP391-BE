
using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Auth;
using System.Text.RegularExpressions;

namespace PlatformFlower.Services.User.Auth
{
    /// <summary>
    /// Centralized validation logic for Authentication operations
    /// Contains ALL validation rules - both format and business logic
    /// </summary>
    public static class AuthValidation
    {
        #region Registration Validation

        /// <summary>
        /// Complete validation for user registration
        /// </summary>
        public static async Task ValidateRegistrationAsync(RegisterRequest request, FlowershopContext context)
        {
            // 1. Basic field validation (moved from DTO)
            ValidateRegistrationFields(request);

            // 2. Business logic validation
            await ValidateUsernameUniqueness(request.Username, context);
            await ValidateEmailUniqueness(request.Email, context);
        }

        /// <summary>
        /// Validate basic registration fields (moved from DTO Data Annotations)
        /// </summary>
        private static void ValidateRegistrationFields(RegisterRequest request)
        {
            // Username validation
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

            // Password validation
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

            // Email validation
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

        #endregion

        #region Login Validation

        /// <summary>
        /// Complete validation for user login
        /// </summary>
        public static void ValidateLogin(LoginRequest request)
        {
            // Basic field validation (moved from DTO)
            ValidateLoginFields(request);
        }

        /// <summary>
        /// Validate basic login fields (moved from DTO Data Annotations)
        /// </summary>
        private static void ValidateLoginFields(LoginRequest request)
        {
            // Username validation
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                throw new ArgumentException("Username is required");
            }

            if (request.Username.Length > 255)
            {
                throw new ArgumentException("Username must not exceed 255 characters");
            }

            // Password validation
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ArgumentException("Password is required");
            }

            if (request.Password.Length > 255)
            {
                throw new ArgumentException("Password must not exceed 255 characters");
            }
        }

        #endregion

        #region Reset Password Validation

        /// <summary>
        /// Complete validation for password reset
        /// </summary>
        public static void ValidateResetPassword(ResetPasswordRequest request)
        {
            // Basic field validation (moved from DTO)
            ValidateResetPasswordFields(request);
        }

        /// <summary>
        /// Validate reset password fields (moved from DTO Data Annotations)
        /// </summary>
        private static void ValidateResetPasswordFields(ResetPasswordRequest request)
        {
            // Token validation
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                throw new ArgumentException("Reset token is required");
            }

            // New password validation
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

            // Confirm password validation
            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                throw new ArgumentException("Confirm password is required");
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new ArgumentException("Password and confirm password do not match");
            }
        }

        #endregion

        #region Forgot Password Validation

        /// <summary>
        /// Complete validation for forgot password
        /// </summary>
        public static void ValidateForgotPassword(ForgotPasswordRequest request)
        {
            // Email validation
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required");
            }

            if (!IsValidEmailFormat(request.Email))
            {
                throw new ArgumentException("Invalid email format");
            }
        }

        #endregion

        #region Business Logic Validation

        /// <summary>
        /// Validate username uniqueness in database
        /// </summary>
        private static async Task ValidateUsernameUniqueness(string username, FlowershopContext context)
        {
            if (await context.Users.AnyAsync(u => u.Username == username))
            {
                throw new InvalidOperationException("Username already exists");
            }
        }

        /// <summary>
        /// Validate email uniqueness in database
        /// </summary>
        private static async Task ValidateEmailUniqueness(string email, FlowershopContext context)
        {
            if (await context.Users.AnyAsync(u => u.Email == email))
            {
                throw new InvalidOperationException("Email already exists");
            }
        }

        #endregion

        #region Format Validation Helpers

        /// <summary>
        /// Validate email format using regex
        /// </summary>
        private static bool IsValidEmailFormat(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            return emailRegex.IsMatch(email);
        }

        /// <summary>
        /// Validate username format (alphanumeric and underscore only)
        /// </summary>
        private static bool IsValidUsernameFormat(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            var usernameRegex = new Regex(@"^[a-zA-Z0-9_]+$");
            return usernameRegex.IsMatch(username);
        }

        /// <summary>
        /// Validate password complexity
        /// </summary>
        private static bool IsValidPasswordComplexity(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Must contain at least one uppercase, one lowercase, and one digit
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);

            return hasUpper && hasLower && hasDigit;
        }

        #endregion
    }
}
