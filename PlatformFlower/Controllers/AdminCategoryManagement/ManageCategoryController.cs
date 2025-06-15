using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Category;
using PlatformFlower.Services.Admin.CategoryManagement;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;

namespace PlatformFlower.Controllers.AdminCategoryManagement
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Roles = "admin")]
    public class ManageCategoryController : ControllerBase
    {
        private readonly ICategoryManagementService _categoryManagementService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public ManageCategoryController(
            ICategoryManagementService categoryManagementService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _categoryManagementService = categoryManagementService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpPost("manage")]
        public async Task<ActionResult<ApiResponse<CategoryResponse>>> ManageCategory([FromBody] CreateCategoryRequest request)
        {
            try
            {
                // Determine operation type for logging
                string operationType = request.CategoryId == null || request.CategoryId == 0
                    ? "CREATE"
                    : request.IsDeleted
                        ? "DELETE"
                        : "UPDATE";

                _logger.LogInformation($"Admin managing category - Operation: {operationType}, CategoryName: {request.CategoryName}");

                var result = await _categoryManagementService.ManageCategoryAsync(request);

                // Determine operation type for response message
                string operation;
                string message;

                if (request.CategoryId == null || request.CategoryId == 0)
                {
                    // For CREATE operation, check if it was a reactivation
                    operation = "created";
                    message = result.CreatedAt < result.UpdatedAt
                        ? "Category reactivated successfully (was previously inactive)"
                        : "Category created successfully";
                }
                else if (request.IsDeleted)
                {
                    operation = "deleted";
                    message = "Category deleted successfully (set to inactive)";
                }
                else
                {
                    operation = "updated";
                    message = "Category updated successfully";
                }

                var response = _responseService.CreateSuccessResponse(result, message);

                _logger.LogInformation($"Category {operation} successfully - ID: {result.CategoryId}, Name: {result.CategoryName}");
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Category management validation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<CategoryResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Category management operation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<CategoryResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during category management: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<CategoryResponse>(
                    "An error occurred while processing the request"
                );
                return StatusCode(500, response);
            }
        }
    }
}
