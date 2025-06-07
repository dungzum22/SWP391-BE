using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;

namespace PlatformFlower.Services.Common.Response
{
    public interface IResponseService
    {
        ActionResult<ApiResponse<T>> Success<T>(T data, string message = "Operation completed successfully");
        ActionResult<ApiResponse<T>> BadRequest<T>(string message, object errors = null);
        ActionResult<ApiResponse<T>> Unauthorized<T>(string message);
        ActionResult<ApiResponse<T>> NotFound<T>(string message = "Resource not found");
        ActionResult<ApiResponse<T>> Conflict<T>(string message);
        ActionResult<ApiResponse<T>> InternalServerError<T>(string message = "An internal server error occurred");

        // Additional methods for creating response objects without ActionResult
        ApiResponse<T> CreateSuccessResponse<T>(T data, string message = "Operation completed successfully");
        ApiResponse<T> CreateErrorResponse<T>(string message, object errors = null);
    }
}
