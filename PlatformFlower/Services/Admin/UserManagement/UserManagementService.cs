using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.User;
using PlatformFlower.Services.Common.Logging;

namespace PlatformFlower.Services.Admin.UserManagement
{
    public class UserManagementService : IUserManagementService
    {
        private readonly FlowershopContext _context;
        private readonly IAppLogger _logger;

        public UserManagementService(FlowershopContext context, IAppLogger logger)
        {
            _context = context;
            _logger = logger;
        }



        public async Task<List<UserListRequest>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("Getting all users list without pagination or filtering");

                var users = await _context.Users
                    .Include(u => u.UserInfos)
                    .OrderByDescending(u => u.CreatedDate)
                    .Select(u => new UserListRequest
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Email = u.Email,
                        Type = u.Type,
                        CreatedAt = u.CreatedDate,
                        UpdatedAt = u.CreatedDate,
                        IsActive = u.Status == "active" || u.Status == null,
                        UserInfo = u.UserInfos.Any() ? new UserInfoManagement
                        {
                            FullName = u.UserInfos.First().FullName,
                            Phone = null,
                            Address = u.UserInfos.First().Address,
                            DateOfBirth = u.UserInfos.First().BirthDate.HasValue ? u.UserInfos.First().BirthDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                            Gender = u.UserInfos.First().Sex,
                            Avatar = u.UserInfos.First().Avatar
                        } : null
                    })
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {users.Count} users");
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all users list: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<UserDetailResponse?> GetUserByIdAsync(int userId)
        {
            try
            {
                _logger.LogInformation($"Getting user details for ID: {userId}");

                var user = await _context.Users
                    .Include(u => u.UserInfos)
                    .Include(u => u.Sellers)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {userId}");
                    return null;
                }

                var userInfo = user.UserInfos.FirstOrDefault();
                var seller = user.Sellers.FirstOrDefault();

                var result = new UserDetailResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    Type = user.Type,
                    CreatedAt = user.CreatedDate,
                    UpdatedAt = user.CreatedDate,
                    IsActive = user.Status == "active" || user.Status == null,
                    UserInfo = userInfo != null ? new UserInfoManagement
                    {
                        FullName = userInfo.FullName,
                        Phone = null,
                        Address = userInfo.Address,
                        DateOfBirth = userInfo.BirthDate?.ToDateTime(TimeOnly.MinValue),
                        Gender = userInfo.Sex,
                        Avatar = userInfo.Avatar
                    } : null,
                    SellerInfo = seller != null ? new SellerInfo
                    {
                        SellerId = seller.SellerId,
                        ShopName = seller.ShopName,
                        AddressSeller = seller.AddressSeller,
                        Role = seller.Role,
                        TotalProduct = seller.TotalProduct,
                        CreatedAt = seller.CreatedAt
                    } : null
                };

                _logger.LogInformation($"Retrieved user details for ID: {userId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user details for ID {userId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<UserDetailResponse> ToggleUserStatusAsync(int userId, string reason)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }


                string newStatus;
                string action;

                if (user.Status == "inactive")
                {
                    newStatus = "active";
                    action = "Activating";
                }
                else
                {
                    newStatus = "inactive";
                    action = "Deactivating";
                }

                _logger.LogInformation($"{action} user ID: {userId}, Reason: {reason}");

                user.Status = newStatus;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User status changed successfully - ID: {userId}, New Status: {newStatus}");

                var result = await GetUserByIdAsync(userId);
                return result!;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error toggling user status for ID {userId}: {ex.Message}", ex);
                throw;
            }
        }


    }
}
