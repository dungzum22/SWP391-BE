using Microsoft.AspNetCore.Mvc.ModelBinding;
using PlatformFlower.Models;

namespace PlatformFlower.Services.Common.Validation
{
    public interface IValidationService
    {
        ApiResponse<T> ValidateModelState<T>(ModelStateDictionary modelState);
        bool IsModelStateValid(ModelStateDictionary modelState);
        ApiResponse<T> CreateValidationErrorResponse<T>(string message, object errors);

        // Additional validation methods for user registration
        bool IsValidEmail(string email);
        bool IsValidPassword(string password);
        bool IsValidUsername(string username);
    }
}
