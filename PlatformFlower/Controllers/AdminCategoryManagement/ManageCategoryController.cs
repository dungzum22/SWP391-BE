using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models.DTOs;
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
        public async Task<ActionResult<CategoryResponseDto>> ManageCategory([FromBody] CategoryManageRequestDto request)
        {
            try
            {
                var result = await _categoryManagementService.ManageCategoryAsync(request);
                
                // Determine operation type for response message
                string operation = request.CategoryId == null || request.CategoryId == 0 ? "created" :
                                 request.IsDeleted ? "deleted" : "updated";
                
                return Ok(new
                {
                    success = true,
                    message = $"Category {operation} successfully",
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
