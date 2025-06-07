using Microsoft.AspNetCore.Mvc.ModelBinding;
using PlatformFlower.Models;

namespace PlatformFlower.Services.Common.Validation
{
    public interface IValidationService
    {
        ApiResponse<T> ValidateModelState<T>(ModelStateDictionary modelState);
        bool IsModelStateValid(ModelStateDictionary modelState);
        ApiResponse<T> CreateValidationErrorResponse<T>(string message, object errors);
    }
}
