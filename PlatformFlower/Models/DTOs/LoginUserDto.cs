using System.ComponentModel.DataAnnotations;

namespace PlatformFlower.Models.DTOs
{
    public class LoginUserDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(255, ErrorMessage = "Username must not exceed 255 characters")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, ErrorMessage = "Password must not exceed 255 characters")]
        public string Password { get; set; } = null!;
    }
}
