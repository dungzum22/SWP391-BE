using Microsoft.AspNetCore.Mvc.ModelBinding;
using PlatformFlower.Models;
using System.Text.RegularExpressions;

namespace PlatformFlower.Services.Common.Validation
{
    public class ValidationService : IValidationService
    {
        public ApiResponse<T> ValidateModelState<T>(ModelStateDictionary modelState)
        {
            if (IsModelStateValid(modelState))
            {
                return null!;
            }

            var errors = ExtractModelStateErrors(modelState);
            return CreateValidationErrorResponse<T>("Invalid input data", errors);
        }

        public bool IsModelStateValid(ModelStateDictionary modelState)
        {
            return modelState.IsValid;
        }

        public ApiResponse<T> CreateValidationErrorResponse<T>(string message, object errors)
        {
            return ApiResponse<T>.ErrorResult(message, errors);
        }

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            return emailRegex.IsMatch(email);
        }

        public bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            return password.Length >= 6;
        }

        public bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            var usernameRegex = new Regex(@"^[a-zA-Z0-9_]{3,255}$");
            return usernameRegex.IsMatch(username);
        }

        private static Dictionary<string, IEnumerable<string>> ExtractModelStateErrors(ModelStateDictionary modelState)
        {
            return modelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage)
                );
        }
    }
}
