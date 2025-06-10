using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;

namespace PlatformFlower.Services.Common.Response
{
    public class ResponseService : IResponseService
    {
        public ActionResult<ApiResponse<T>> Success<T>(T data, string message = "Operation completed successfully")
        {
            var response = ApiResponse<T>.SuccessResult(data, message);
            return new OkObjectResult(response);
        }

        public ActionResult<ApiResponse<T>> BadRequest<T>(string message, object errors = null)
        {
            var response = ApiResponse<T>.ErrorResult(message, errors);
            return new BadRequestObjectResult(response);
        }

        public ActionResult<ApiResponse<T>> Unauthorized<T>(string message)
        {
            var response = ApiResponse<T>.ErrorResult(message);
            return new UnauthorizedObjectResult(response);
        }

        public ActionResult<ApiResponse<T>> NotFound<T>(string message = "Resource not found")
        {
            var response = ApiResponse<T>.ErrorResult(message);
            return new NotFoundObjectResult(response);
        }

        public ActionResult<ApiResponse<T>> Conflict<T>(string message)
        {
            var response = ApiResponse<T>.ErrorResult(message);
            return new ConflictObjectResult(response);
        }

        public ActionResult<ApiResponse<T>> InternalServerError<T>(string message = "An internal server error occurred")
        {
            var response = ApiResponse<T>.ErrorResult(message);
            return new ObjectResult(response) { StatusCode = 500 };
        }

        public ApiResponse<T> CreateSuccessResponse<T>(T data, string message = "Operation completed successfully")
        {
            return ApiResponse<T>.SuccessResult(data, message);
        }

        public ApiResponse<T> CreateErrorResponse<T>(string message, object errors = null)
        {
            return ApiResponse<T>.ErrorResult(message, errors);
        }
    }
}
