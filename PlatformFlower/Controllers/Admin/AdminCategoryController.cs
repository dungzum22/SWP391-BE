using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Admin.CategoryManagement;

namespace PlatformFlower.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Roles = "admin")]
    public class AdminCategoryController : ControllerBase
    {
        private readonly ICategoryManagementService _categoryManagementService;

        public AdminCategoryController(ICategoryManagementService categoryManagementService)
        {
            _categoryManagementService = categoryManagementService;
        }

        /// <summary>
        /// Universal Category Management API - Handles CREATE, UPDATE, DELETE in one endpoint
        /// </summary>
        /// <param name="request">Category management request</param>
        /// <returns>Category response</returns>
        /// <remarks>
        /// Operations:
        /// - CREATE: Set CategoryId = 0 or null, provide CategoryName
        /// - UPDATE: Set CategoryId > 0, provide CategoryName and/or Status, IsDeleted = false
        /// - DELETE: Set CategoryId > 0, IsDeleted = true
        /// 
        /// Examples:
        /// 
        /// CREATE:
        /// {
        ///   "categoryId": 0,
        ///   "categoryName": "New Category",
        ///   "status": "active",
        ///   "isDeleted": false
        /// }
        ///
        /// UPDATE:
        /// {
        ///   "categoryId": 5,
        ///   "categoryName": "Updated Category Name",
        ///   "status": "active",
        ///   "isDeleted": false
        /// }
        ///
        /// DELETE:
        /// {
        ///   "categoryId": 5,
        ///   "isDeleted": true
        /// }
        /// </remarks>
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

        /// <summary>
        /// Get all categories
        /// </summary>
        /// <returns>List of all categories</returns>
        [HttpGet]
        public async Task<ActionResult<List<CategoryResponseDto>>> GetAllCategories()
        {
            try
            {
                var result = await _categoryManagementService.GetAllCategoriesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Categories retrieved successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving categories",
                    error = "InternalServerError",
                    details = ex.Message
                });
            }
        }

        /// <summary>
        /// Get category by ID
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <returns>Category details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryResponseDto>> GetCategoryById(int id)
        {
            try
            {
                var result = await _categoryManagementService.GetCategoryByIdAsync(id);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Category with ID {id} not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Category retrieved successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving the category",
                    error = "InternalServerError",
                    details = ex.Message
                });
            }
        }
    }
}
