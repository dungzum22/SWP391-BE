using Microsoft.EntityFrameworkCore;
using PlatformFlower.Entities;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Storage;

namespace PlatformFlower.Services.User.Profile
{
    public class ProfileServiceSimple : IProfileService
    {
        private readonly FlowershopContext _context;
        private readonly IStorageService _storageService;
        private readonly IAppLogger _logger;

        public ProfileServiceSimple(
            FlowershopContext context,
            IStorageService storageService,
            IAppLogger logger)
        {
            _context = context;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserInfos)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return null;

            var userInfo = user.UserInfos.FirstOrDefault();
            return MapToUserResponseDto(user, userInfo);
        }

        public async Task<UserResponseDto?> GetUserByUsernameAsync(string username)
        {
            var user = await _context.Users
                .Include(u => u.UserInfos)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return null;

            var userInfo = user.UserInfos.FirstOrDefault();
            return MapToUserResponseDto(user, userInfo);
        }

        public async Task<UserResponseDto> UpdateUserInfoAsync(int userId, UpdateUserInfoDto updateDto)
        {
            try
            {
                _logger.LogInformation($"Updating user info for user ID: {userId}");

                ProfileValidation.ValidateProfileUpdate(updateDto);
                ProfileValidation.ValidateAvatar(updateDto.Avatar);
                var user = await _context.Users
                    .Include(u => u.UserInfos)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    throw new ArgumentException("User not found");
                }

                _logger.LogInformation($"Updating profile for user: {user.Username}, Type: {user.Type} (role cannot be changed via this endpoint)");
                var userInfo = user.UserInfos.FirstOrDefault();
                if (userInfo == null)
                {
                    userInfo = new UserInfo
                    {
                        UserId = userId,
                        Points = 100,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };
                    _context.UserInfos.Add(userInfo);
                }

                string? newAvatarUrl = null;
                if (updateDto.Avatar != null)
                {
                    try
                    {
                        newAvatarUrl = await _storageService.UploadFileAsync(updateDto.Avatar, "avatars");

                        if (!string.IsNullOrEmpty(userInfo.Avatar))
                        {
                            await _storageService.DeleteFileAsync(userInfo.Avatar);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to upload avatar for user {userId}: {ex.Message}", ex);
                        throw new InvalidOperationException("Failed to upload avatar. Please try again.");
                    }
                }
                if (updateDto.FullName != null)
                    userInfo.FullName = updateDto.FullName;

                if (updateDto.Address != null)
                    userInfo.Address = updateDto.Address;

                if (updateDto.BirthDate.HasValue)
                    userInfo.BirthDate = updateDto.BirthDate.Value;

                if (updateDto.Sex != null)
                    userInfo.Sex = updateDto.Sex;

                if (updateDto.IsSeller.HasValue)
                    userInfo.IsSeller = updateDto.IsSeller.Value;

                if (newAvatarUrl != null)
                    userInfo.Avatar = newAvatarUrl;

                userInfo.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"User info updated successfully for user ID: {userId}");

                return MapToUserResponseDto(user, userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user info for user {userId}: {ex.Message}", ex);
                throw;
            }
        }



        private static UserResponseDto MapToUserResponseDto(Entities.User user, UserInfo? userInfo)
        {
            return new UserResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Type = user.Type,
                CreatedDate = user.CreatedDate,
                Status = user.Status,
                UserInfo = userInfo != null ? new UserInfoDto
                {
                    UserInfoId = userInfo.UserInfoId,
                    FullName = userInfo.FullName,
                    Address = userInfo.Address,
                    BirthDate = userInfo.BirthDate,
                    Sex = userInfo.Sex,
                    IsSeller = userInfo.IsSeller,
                    Avatar = userInfo.Avatar,
                    Points = userInfo.Points,
                    CreatedDate = userInfo.CreatedDate,
                    UpdatedDate = userInfo.UpdatedDate
                } : null
            };
        }
    }
}
