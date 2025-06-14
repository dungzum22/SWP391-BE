using System.ComponentModel.DataAnnotations;

namespace PlatformFlower.Models.DTOs.Auth
{
    public class RegisterUserDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(255, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 255 characters")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email must not exceed 255 characters")]
        public string Email { get; set; } = null!;
    }
}
