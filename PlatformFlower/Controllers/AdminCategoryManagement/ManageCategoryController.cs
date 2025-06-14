using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models.DTOs.Category;
using PlatformFlower.Services.Admin.CategoryManagement;

namespace PlatformFlower.Controllers.AdminCategoryManagement
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Roles = "admin")]
    public class ManageCategoryController : ControllerBase
    {
        private readonly ICategoryManagementService _categoryManagementService;

        public ManageCategoryController(ICategoryManagementService categoryManagementService)
        {
            _categoryManagementService = categoryManagementService;
        }

        [HttpPost("manage")]
        public async Task<ActionResult<CategoryResponse>> ManageCategory([FromBody] CreateCategoryRequest request)
        {
            try
            {
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

                return Ok(new
                {
                    success = true,
                    message = message,
                    operation = operation,
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    error = "ValidationError"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    error = "OperationError"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing the request",
                    error = "InternalServerError",
                    details = ex.Message
                });
            }
        }
    }
}
