using System.ComponentModel.DataAnnotations;

namespace PlatformFlower.Models.DTOs.Category
{
    public class UpdateCategoryRequest
    {
        /// <summary>
        /// Category ID - required for UPDATE
        /// </summary>
        [Required(ErrorMessage = "Category ID is required for update")]
        public int CategoryId { get; set; }

        /// <summary>
        /// Category name - required for UPDATE
        /// </summary>
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(255, ErrorMessage = "Category name cannot exceed 255 characters")]
        public string CategoryName { get; set; } = null!;

        /// <summary>
        /// Status - 'active' or 'inactive'
        /// </summary>
        [StringLength(20)]
        public string? Status { get; set; }
    }
}
