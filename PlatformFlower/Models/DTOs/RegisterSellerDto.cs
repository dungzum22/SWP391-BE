using System.ComponentModel.DataAnnotations;

namespace PlatformFlower.Models.DTOs
{
    public class RegisterSellerDto
    {
        [Required(ErrorMessage = "Shop name is required")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Shop name must be between 2 and 255 characters")]
        public string ShopName { get; set; } = null!;

        [Required(ErrorMessage = "Seller address is required")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Seller address must be between 5 and 255 characters")]
        public string AddressSeller { get; set; } = null!;

        [Required(ErrorMessage = "Role is required")]
        [RegularExpression("^(individual|enterprise)$", ErrorMessage = "Role must be 'individual' or 'enterprise'")]
        public string Role { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Introduction must not exceed 1000 characters")]
        public string? Introduction { get; set; }
    }
}
