using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlatformFlower.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlatformFlower.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly bool _isDevelopment;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _isDevelopment = env.IsDevelopment();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = exception switch
            {
                // 400 Bad Request
                ArgumentException => CreateResponse(context, HttpStatusCode.BadRequest, "Invalid argument provided", exception),
                FormatException => CreateResponse(context, HttpStatusCode.BadRequest, "Invalid format", exception),
                JsonException => CreateResponse(context, HttpStatusCode.BadRequest, "Invalid JSON format", exception),
                
                // 401 Unauthorized
                UnauthorizedAccessException => CreateResponse(context, HttpStatusCode.Unauthorized, "Unauthorized access", exception),
                SecurityException => CreateResponse(context, HttpStatusCode.Unauthorized, "Security violation", exception),
                
                // 403 Forbidden
                System.Security.Authentication.AuthenticationException => CreateResponse(context, HttpStatusCode.Forbidden, "Authentication failed", exception),
                
                // 404 Not Found
                KeyNotFoundException => CreateResponse(context, HttpStatusCode.NotFound, "Resource not found", exception),
                FileNotFoundException => CreateResponse(context, HttpStatusCode.NotFound, "File not found", exception),
                
                // 409 Conflict
                InvalidOperationException => CreateResponse(context, HttpStatusCode.Conflict, "Operation cannot be performed", exception),
                
                // 422 Unprocessable Entity
                ValidationException => CreateResponse(context, HttpStatusCode.UnprocessableEntity, "Validation failed", exception),
                
                // 429 Too Many Requests
                TaskCanceledException => CreateResponse(context, HttpStatusCode.TooManyRequests, "Request was canceled due to timeout or rate limiting", exception),
                
                // Database Errors - 500 but with specific messages
                DbUpdateException => CreateResponse(context, HttpStatusCode.InternalServerError, "Database update error", exception),
                // DbUpdateConcurrencyException => CreateResponse(context, HttpStatusCode.Conflict, "Database concurrency conflict", exception),
                SqlException sqlEx => HandleSqlException(context, sqlEx),
                
                // IO Errors
                IOException => CreateResponse(context, HttpStatusCode.InternalServerError, "IO operation failed", exception),
                
                // Default - 500 Internal Server Error
                _ => CreateResponse(context, HttpStatusCode.InternalServerError, "An unexpected error occurred", exception)
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _isDevelopment
            }));
        }

        private ApiResponse<object> HandleSqlException(HttpContext context, SqlException sqlException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            
            // Xử lý các mã lỗi SQL Server phổ biến
            string message = sqlException.Number switch
            {
                2627 => "Unique constraint violation", // Unique constraint error
                547 => "Constraint check violation",   // Constraint check violation
                2601 => "Duplicate key violation",     // Cannot insert duplicate key
                4060 => "Database access denied",      // Cannot open database
                18456 => "Login failed",               // Login failed for user
                233 => "Database connection failed",   // No connection could be made
                53 => "Server not found",              // Server not found or not accessible
                _ => "Database error occurred"
            };
            
            return CreateResponse(context, HttpStatusCode.InternalServerError, message, sqlException);
        }

        private ApiResponse<object> CreateResponse(HttpContext context, HttpStatusCode statusCode, string message, Exception exception)
        {
            context.Response.StatusCode = (int)statusCode;
            
            // Chỉ trả về chi tiết lỗi trong môi trường development
            object errorDetails = null;
            
            if (_isDevelopment)
            {
                errorDetails = new
                {
                    ExceptionType = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    Source = exception.Source,
                    InnerException = exception.InnerException?.Message
                };
            }

            return ApiResponse<object>.ErrorResult(message, errorDetails);
        }
    }

    // Định nghĩa class ValidationException nếu chưa có
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}

