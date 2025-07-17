using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Voucher;
using PlatformFlower.Services.Common.Logging;

namespace PlatformFlower.Services.Admin.VoucherManagement
{
    public class AdminVoucherService : IAdminVoucherService
    {
        private readonly FlowershopContext _context;
        private readonly IAppLogger _logger;

        public AdminVoucherService(FlowershopContext context, IAppLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<VoucherResponse> ManageVoucherAsync(CreateVoucherRequest request)
        {
            if (request.UserVoucherStatusId == null || request.UserVoucherStatusId == 0)
            {
                await AdminVoucherValidation.ValidateCreateVoucherAsync(request, _context);
                return await CreateVoucherAsync(request);
            }
            else if (request.IsDeleted)
            {
                await AdminVoucherValidation.ValidateDeleteVoucherAsync(request.UserVoucherStatusId.Value, _context);
                return await DeleteVoucherAsync(request.UserVoucherStatusId.Value);
            }
            else
            {
                await AdminVoucherValidation.ValidateUpdateVoucherAsync(request, _context);
                return await UpdateVoucherAsync(request);
            }
        }

        public async Task<List<VoucherResponse>> GetAllVouchersAsync()
        {
            try
            {
                _logger.LogInformation("Admin getting all vouchers");

                var vouchers = await _context.UserVoucherStatuses
                    .Where(v => !v.IsDeleted)
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync();

                var result = vouchers.Select(MapToVoucherResponse).ToList();

                _logger.LogInformation($"Successfully retrieved {result.Count} vouchers for admin");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all vouchers for admin: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<VoucherResponse?> GetVoucherByIdAsync(int voucherStatusId)
        {
            try
            {
                _logger.LogInformation($"Admin getting voucher {voucherStatusId}");

                var voucher = await _context.UserVoucherStatuses
                    .FirstOrDefaultAsync(v => v.UserVoucherStatusId == voucherStatusId);

                if (voucher != null)
                {
                    _logger.LogInformation($"Successfully retrieved voucher - ID: {voucher.UserVoucherStatusId}, Code: {voucher.VoucherCode}");
                    return MapToVoucherResponse(voucher);
                }

                _logger.LogWarning($"Voucher with ID {voucherStatusId} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher {voucherStatusId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> UseVoucherAsync(int voucherStatusId, int userId)
        {
            try
            {
                _logger.LogInformation($"User {userId} attempting to use voucher {voucherStatusId}");

                var voucher = await _context.UserVoucherStatuses
                    .FirstOrDefaultAsync(v => v.UserVoucherStatusId == voucherStatusId && v.UserInfoId == userId);

                if (voucher == null)
                {
                    _logger.LogWarning($"Voucher {voucherStatusId} not found for user {userId}");
                    return false;
                }

                if (!IsVoucherUsable(voucher))
                {
                    _logger.LogWarning($"Voucher {voucherStatusId} is not usable for user {userId}");
                    return false;
                }

                voucher.UsageCount = (voucher.UsageCount ?? 0) + 1;
                if (voucher.RemainingCount.HasValue)
                {
                    voucher.RemainingCount--;
                }

                if (voucher.RemainingCount == 0)
                {
                    voucher.Status = "inactive";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully used voucher {voucherStatusId} for user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error using voucher {voucherStatusId} for user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<VoucherResponse?> GetVoucherByCodeAsync(string voucherCode, int? userId = null)
        {
            try
            {
                _logger.LogInformation($"Getting voucher by code: {voucherCode} for user: {userId}");

                var query = _context.UserVoucherStatuses.AsQueryable();

                if (userId.HasValue)
                {
                    query = query.Where(v => v.VoucherCode == voucherCode && v.UserInfoId == userId.Value);
                }
                else
                {
                    query = query.Where(v => v.VoucherCode == voucherCode);
                }

                var voucher = await query.FirstOrDefaultAsync();

                if (voucher != null)
                {
                    _logger.LogInformation($"Found voucher with code {voucherCode}");
                    return MapToVoucherResponse(voucher);
                }

                _logger.LogWarning($"Voucher with code {voucherCode} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher by code {voucherCode}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<VoucherStatsResponse> GetVoucherStatsAsync(string voucherCode)
        {
            try
            {
                _logger.LogInformation($"Getting voucher stats for code: {voucherCode}");

                var vouchers = await _context.UserVoucherStatuses
                    .Include(v => v.UserInfo)
                    .ThenInclude(ui => ui!.User)
                    .Where(v => v.VoucherCode == voucherCode)
                    .ToListAsync();

                if (!vouchers.Any())
                {
                    throw new InvalidOperationException($"No vouchers found with code: {voucherCode}");
                }

                var totalUsers = vouchers.Count;
                var usedCount = vouchers.Count(v => (v.UsageCount ?? 0) > 0);
                var totalRemainingCount = vouchers.Sum(v => v.RemainingCount ?? 0);

                var userStats = vouchers.Select(v => new VoucherUserStats
                {
                    UserInfoId = v.UserInfoId ?? 0,
                    UserName = v.UserInfo?.User?.Username,
                    Email = v.UserInfo?.User?.Email,
                    UsageCount = v.UsageCount,
                    RemainingCount = v.RemainingCount,
                    Status = v.Status
                }).ToList();

                var stats = new VoucherStatsResponse
                {
                    VoucherCode = voucherCode,
                    TotalUsers = totalUsers,
                    UsedCount = usedCount,
                    RemainingCount = totalRemainingCount,
                    UsagePercentage = totalUsers > 0 ? (double)usedCount / totalUsers * 100 : 0,
                    UserStats = userStats
                };

                _logger.LogInformation($"Successfully retrieved stats for voucher {voucherCode}");
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher stats for {voucherCode}: {ex.Message}", ex);
                throw;
            }
        }

        private async Task<VoucherResponse> CreateVoucherAsync(CreateVoucherRequest request)
        {
            _logger.LogInformation($"Creating voucher with code: {request.VoucherCode}");

            // Get all active users
            var activeUsers = await _context.Users
                .Where(u => u.Status == "active" && u.Type == "user")
                .ToListAsync();

            if (!activeUsers.Any())
            {
                throw new InvalidOperationException("No active users found to create vouchers for");
            }

            // Get or create UserInfo for each active user
            var userInfos = new List<Entities.UserInfo>();
            foreach (var user in activeUsers)
            {
                var userInfo = await _context.UserInfos
                    .FirstOrDefaultAsync(ui => ui.UserId == user.UserId);

                if (userInfo == null)
                {
                    // Create UserInfo if it doesn't exist
                    userInfo = new Entities.UserInfo
                    {
                        UserId = user.UserId,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };
                    _context.UserInfos.Add(userInfo);
                    await _context.SaveChangesAsync(); // Save to get UserInfoId
                    _logger.LogInformation($"Created UserInfo for user {user.UserId} during voucher creation");
                }
                userInfos.Add(userInfo);
            }

            var createdVouchers = new List<Entities.UserVoucherStatus>();

            foreach (var userInfo in userInfos)
            {
                var voucher = new Entities.UserVoucherStatus
                {
                    VoucherCode = request.VoucherCode!,
                    Discount = request.Discount!.Value,
                    Description = request.Description,
                    StartDate = request.StartDate!.Value,
                    EndDate = request.EndDate!.Value,
                    UsageLimit = request.UsageLimit,
                    UsageCount = 0,
                    RemainingCount = request.RemainingCount ?? request.UsageLimit,
                    CreatedAt = DateTime.Now,
                    Status = request.Status ?? "active",
                    IsDeleted = false,
                    UserInfoId = userInfo.UserInfoId
                };

                createdVouchers.Add(voucher);
            }

            _context.UserVoucherStatuses.AddRange(createdVouchers);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created voucher '{request.VoucherCode}' for {createdVouchers.Count} active users");

            return MapToVoucherResponse(createdVouchers.First());
        }

        private async Task<VoucherResponse> UpdateVoucherAsync(CreateVoucherRequest request)
        {
            var vouchers = await _context.UserVoucherStatuses
                .Where(v => v.VoucherCode == GetVoucherCodeById(request.UserVoucherStatusId!.Value))
                .ToListAsync();

            if (!vouchers.Any())
            {
                throw new InvalidOperationException($"No vouchers found with ID {request.UserVoucherStatusId}");
            }

            foreach (var voucher in vouchers)
            {
                if (!string.IsNullOrEmpty(request.VoucherCode))
                    voucher.VoucherCode = request.VoucherCode;
                if (request.Discount.HasValue)
                    voucher.Discount = request.Discount.Value;
                if (!string.IsNullOrEmpty(request.Description))
                    voucher.Description = request.Description;
                if (request.StartDate.HasValue)
                    voucher.StartDate = request.StartDate.Value;
                if (request.EndDate.HasValue)
                    voucher.EndDate = request.EndDate.Value;
                if (request.UsageLimit.HasValue)
                    voucher.UsageLimit = request.UsageLimit.Value;
                if (request.RemainingCount.HasValue)
                    voucher.RemainingCount = request.RemainingCount.Value;
                if (!string.IsNullOrEmpty(request.Status))
                    voucher.Status = request.Status;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated voucher with ID: {request.UserVoucherStatusId}");
            return MapToVoucherResponse(vouchers.First());
        }

        private async Task<VoucherResponse> DeleteVoucherAsync(int voucherStatusId)
        {
            var vouchers = await _context.UserVoucherStatuses
                .Where(v => v.VoucherCode == GetVoucherCodeById(voucherStatusId))
                .ToListAsync();

            if (!vouchers.Any())
            {
                throw new InvalidOperationException($"No vouchers found with ID {voucherStatusId}");
            }

            foreach (var voucher in vouchers)
            {
                voucher.IsDeleted = true;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Soft deleted voucher with ID: {voucherStatusId}");
            return MapToVoucherResponse(vouchers.First());
        }

        private string GetVoucherCodeById(int voucherStatusId)
        {
            var voucher = _context.UserVoucherStatuses
                .FirstOrDefault(v => v.UserVoucherStatusId == voucherStatusId);
            return voucher?.VoucherCode ?? throw new InvalidOperationException($"Voucher with ID {voucherStatusId} not found");
        }

        private bool IsVoucherUsable(Entities.UserVoucherStatus voucher)
        {
            var now = DateTime.Now;
            return !voucher.IsDeleted &&
                   voucher.Status == "active" &&
                   now >= voucher.StartDate &&
                   now <= voucher.EndDate &&
                   (voucher.RemainingCount == null || voucher.RemainingCount > 0);
        }

        private VoucherResponse MapToVoucherResponse(Entities.UserVoucherStatus voucher)
        {
            var currentStatus = GetCurrentVoucherStatus(voucher);

            return new VoucherResponse
            {
                UserVoucherStatusId = voucher.UserVoucherStatusId,
                VoucherCode = voucher.VoucherCode,
                Discount = voucher.Discount,
                Description = voucher.Description,
                StartDate = voucher.StartDate,
                EndDate = voucher.EndDate,
                UsageLimit = voucher.UsageLimit,
                UsageCount = voucher.UsageCount,
                RemainingCount = voucher.RemainingCount,
                CreatedAt = voucher.CreatedAt,
                Status = currentStatus,
                IsDeleted = voucher.IsDeleted
            };
        }

        private string GetCurrentVoucherStatus(Entities.UserVoucherStatus voucher)
        {
            if (voucher.IsDeleted)
                return "deleted";
            if (voucher.Status == "inactive")
                return "inactive";
            if (DateTime.Now < voucher.StartDate)
                return "upcoming";
            if (DateTime.Now > voucher.EndDate)
                return "expired";
            if (voucher.RemainingCount != null && voucher.RemainingCount <= 0)
                return "used_up";
            return "active";
        }
    }
}
