using System.ComponentModel.DataAnnotations;

namespace PlatformFlower.Models.DTOs
{
    public class CategoryManageRequestDto
    {
        /// <summary>
        /// Category ID - 0 or null for CREATE, > 0 for UPDATE/DELETE
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// Category name - required for CREATE and UPDATE
        /// </summary>
        [StringLength(255, ErrorMessage = "Category name cannot exceed 255 characters")]
        public string? CategoryName { get; set; }

        /// <summary>
        /// Status - 'active' or 'inactive'
        /// </summary>
        [StringLength(20)]
        public string? Status { get; set; }

        /// <summary>
        /// Set to true for DELETE operation (soft delete)
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }

    public class CategoryResponseDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int FlowerCount { get; set; } // Number of flowers in this category
    }


}
