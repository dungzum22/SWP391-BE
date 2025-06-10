using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs;
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

        public async Task<PaginatedUsersResponseDto> GetUsersAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? userType = null, bool? isActive = null)
        {
            try
            {
                _logger.LogInformation($"Getting users list - Page: {pageNumber}, Size: {pageSize}, Search: {searchTerm}, Type: {userType}, Active: {isActive}");

                var query = _context.Users
                    .Include(u => u.UserInfos)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(u =>
                        u.Username.Contains(searchTerm) ||
                        u.Email.Contains(searchTerm) ||
                        (u.UserInfos.Any() && u.UserInfos.First().FullName != null && u.UserInfos.First().FullName.Contains(searchTerm)));
                }

                if (!string.IsNullOrEmpty(userType))
                {
                    query = query.Where(u => u.Type == userType);
                }

                if (isActive.HasValue)
                {
                    // Use Status field to determine if user is active
                    if (isActive.Value)
                    {
                        query = query.Where(u => u.Status == "active" || u.Status == null);
                    }
                    else
                    {
                        query = query.Where(u => u.Status == "inactive");
                    }
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var users = await query
                    .OrderByDescending(u => u.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserListResponseDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Email = u.Email,
                        Type = u.Type,
                        CreatedAt = u.CreatedDate,
                        UpdatedAt = u.CreatedDate, // Use CreatedDate as UpdatedAt since no UpdatedAt field
                        IsActive = u.Status == "active" || u.Status == null,
                        UserInfo = u.UserInfos.Any() ? new UserInfoManagementDto
                        {
                            FullName = u.UserInfos.First().FullName,
                            Phone = null, // UserInfo doesn't have Phone field
                            Address = u.UserInfos.First().Address,
                            DateOfBirth = u.UserInfos.First().BirthDate.HasValue ? u.UserInfos.First().BirthDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                            Gender = u.UserInfos.First().Sex,
                            Avatar = u.UserInfos.First().Avatar
                        } : null
                    })
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {users.Count} users out of {totalCount} total");

                return new PaginatedUsersResponseDto
                {
                    Users = users,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasNextPage = pageNumber < totalPages,
                    HasPreviousPage = pageNumber > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting users list: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<UserDetailResponseDto?> GetUserByIdAsync(int userId)
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

                var result = new UserDetailResponseDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    Type = user.Type,
                    CreatedAt = user.CreatedDate,
                    UpdatedAt = user.CreatedDate, // Use CreatedDate as UpdatedAt
                    IsActive = user.Status == "active" || user.Status == null,
                    UserInfo = userInfo != null ? new UserInfoManagementDto
                    {
                        FullName = userInfo.FullName,
                        Phone = null, // UserInfo doesn't have Phone field
                        Address = userInfo.Address,
                        DateOfBirth = userInfo.BirthDate?.ToDateTime(TimeOnly.MinValue),
                        Gender = userInfo.Sex,
                        Avatar = userInfo.Avatar
                    } : null,
                    SellerInfo = seller != null ? new SellerInfoDto
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

        public async Task<UserDetailResponseDto> ToggleUserStatusAsync(int userId, string reason)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                // Determine new status based on current status
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
