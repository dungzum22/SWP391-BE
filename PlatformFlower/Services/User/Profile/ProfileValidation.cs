using PlatformFlower.Models.DTOs;
using System.Security;

namespace PlatformFlower.Services.User.Profile
{
    public static class ProfileValidation
    {
        public static void ValidateProfileUpdate(UpdateUserInfoDto updateDto)
        {
            var dtoType = updateDto.GetType();
            var properties = dtoType.GetProperties();

            var restrictedProperties = new[] { "Type", "Role", "Status", "UserId", "Password" };

            foreach (var prop in properties)
            {
                if (restrictedProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                {
                    throw new SecurityException($"Attempt to modify restricted property: {prop.Name}");
                }
            }

            if (updateDto.FullName != null && updateDto.FullName.Length > 255)
            {
                throw new ArgumentException("Full name cannot exceed 255 characters");
            }

            if (updateDto.Address != null && updateDto.Address.Length > 500)
            {
                throw new ArgumentException("Address cannot exceed 500 characters");
            }

            if (updateDto.BirthDate.HasValue && updateDto.BirthDate.Value > DateOnly.FromDateTime(DateTime.Now))
            {
                throw new ArgumentException("Birth date cannot be in the future");
            }

            if (updateDto.Sex != null && !new[] { "Male", "Female", "Other" }.Contains(updateDto.Sex))
            {
                throw new ArgumentException("Invalid sex value. Must be Male, Female, or Other");
            }
        }

        public static void ValidateAvatar(IFormFile? avatar)
        {
            if (avatar == null) return;

            if (avatar.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("Avatar file size cannot exceed 5MB");
            }

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(avatar.ContentType.ToLower()))
            {
                throw new ArgumentException("Avatar must be a valid image file (JPEG, PNG, GIF)");
            }
        }
    }
}
