using System.ComponentModel.DataAnnotations;

namespace PlatformFlower.Models.DTOs.User
{
    public class UpdateUserRequest
    {
        [StringLength(255, ErrorMessage = "Full name must not exceed 255 characters")]
        public string? FullName { get; set; }

        [StringLength(500, ErrorMessage = "Address must not exceed 500 characters")]
        public string? Address { get; set; }

        public DateOnly? BirthDate { get; set; }

        [RegularExpression("^(male|female|other)$", ErrorMessage = "Sex must be 'male', 'female', or 'other'")]
        public string? Sex { get; set; }

        public bool? IsSeller { get; set; }

        public IFormFile? Avatar { get; set; }
    }
}
