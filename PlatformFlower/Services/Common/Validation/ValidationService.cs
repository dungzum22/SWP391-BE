using Microsoft.AspNetCore.Mvc.ModelBinding;
using PlatformFlower.Models;

namespace PlatformFlower.Services.Common.Validation
{
    public class ValidationService : IValidationService
    {
        public ApiResponse<T> ValidateModelState<T>(ModelStateDictionary modelState)
        {
            if (IsModelStateValid(modelState))
            {
                return null!; // No validation errors
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

        private Dictionary<string, IEnumerable<string>> ExtractModelStateErrors(ModelStateDictionary modelState)
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
