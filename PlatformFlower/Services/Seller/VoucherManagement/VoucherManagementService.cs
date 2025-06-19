using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Voucher;
using PlatformFlower.Services.Common.Logging;

namespace PlatformFlower.Services.Seller.VoucherManagement
{
    public class VoucherManagementService : IVoucherManagementService
    {
        private readonly FlowershopContext _context;
        private readonly IAppLogger _logger;

        public VoucherManagementService(FlowershopContext context, IAppLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<VoucherResponse> ManageVoucherAsync(CreateVoucherRequest request, int sellerId)
        {
            if (request.UserVoucherStatusId == null || request.UserVoucherStatusId == 0)
            {
                await VoucherValidation.ValidateCreateVoucherAsync(request, _context, sellerId);
                return await CreateVoucherAsync(request, sellerId);
            }
            else if (request.IsDeleted)
            {
                await VoucherValidation.ValidateDeleteVoucherAsync(request.UserVoucherStatusId.Value, _context, sellerId);
                return await DeleteVoucherAsync(request.UserVoucherStatusId.Value);
            }
            else
            {
                await VoucherValidation.ValidateUpdateVoucherAsync(request, _context, sellerId);
                return await UpdateVoucherAsync(request, sellerId);
            }
        }

        public async Task<List<VoucherResponse>> GetAllVouchersAsync(int sellerId)
        {
            try
            {
                _logger.LogInformation($"Getting all vouchers for seller {sellerId}");

                var vouchers = await _context.UserVoucherStatuses
                    .Include(v => v.Shop)
                    .Where(v => v.ShopId == sellerId)
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync();

                await UpdateVoucherStatusesAsync(vouchers);

                var groupedVouchers = vouchers
                    .GroupBy(v => new { v.VoucherCode, v.CreatedAt.Value.Date })
                    .Select(g => new VoucherResponse
                    {
                        UserVoucherStatusId = g.First().UserVoucherStatusId,
                        VoucherCode = g.Key.VoucherCode,
                        Discount = g.First().Discount,
                        Description = g.First().Description,
                        StartDate = g.First().StartDate,
                        EndDate = g.First().EndDate,
                        UsageLimit = g.First().UsageLimit,
                        UsageCount = g.Sum(v => v.UsageCount ?? 0),
                        RemainingCount = g.Sum(v => v.RemainingCount ?? 0),
                        CreatedAt = g.First().CreatedAt,
                        ShopId = g.First().ShopId,
                        ShopName = g.First().Shop?.ShopName,
                        Status = GetGroupStatus(g.ToList()),
                        IsDeleted = g.First().IsDeleted
                    })
                    .OrderByDescending(v => v.CreatedAt)
                    .ToList();

                _logger.LogInformation($"Retrieved {groupedVouchers.Count} voucher campaigns for seller {sellerId} (total {vouchers.Count} individual vouchers)");
                return groupedVouchers;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting vouchers for seller {sellerId}: {ex.Message}");
                throw;
            }
        }

        public async Task<VoucherResponse?> GetVoucherByIdAsync(int voucherStatusId, int sellerId)
        {
            try
            {
                _logger.LogInformation($"Getting voucher {voucherStatusId} for seller {sellerId}");

                var voucher = await _context.UserVoucherStatuses
                    .Include(v => v.Shop)
                    .FirstOrDefaultAsync(v => v.UserVoucherStatusId == voucherStatusId && v.ShopId == sellerId);

                if (voucher != null)
                {
                    _logger.LogInformation($"Successfully retrieved voucher - ID: {voucher.UserVoucherStatusId}, Code: {voucher.VoucherCode}");
                    return MapToVoucherResponse(voucher);
                }

                _logger.LogWarning($"Voucher with ID {voucherStatusId} not found for seller {sellerId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher {voucherStatusId} for seller {sellerId}: {ex.Message}");
                throw;
            }
        }

        private async Task<VoucherResponse> CreateVoucherAsync(CreateVoucherRequest request, int sellerId)
        {
            var activeUsers = await _context.UserInfos
                .Include(ui => ui.User)
                .Where(ui => ui.User != null && ui.User.Status == "active")
                .ToListAsync();

            if (!activeUsers.Any())
            {
                throw new InvalidOperationException("No active users found to create vouchers for");
            }

            var createdVouchers = new List<Entities.UserVoucherStatus>();

            foreach (var user in activeUsers)
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
                    ShopId = sellerId,
                    Status = request.Status ?? "active",
                    IsDeleted = false,
                    UserInfoId = user.UserInfoId
                };

                createdVouchers.Add(voucher);
            }

            _context.UserVoucherStatuses.AddRange(createdVouchers);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created voucher '{request.VoucherCode}' for {createdVouchers.Count} active users");

            return await MapToVoucherResponseWithShop(createdVouchers.First());
        }

        private async Task<VoucherResponse> UpdateVoucherAsync(CreateVoucherRequest request, int sellerId)
        {
            var voucher = await _context.UserVoucherStatuses
                .Include(v => v.Shop)
                .FirstOrDefaultAsync(v => v.UserVoucherStatusId == request.UserVoucherStatusId && v.ShopId == sellerId);

            if (voucher == null)
            {
                throw new InvalidOperationException($"Voucher with ID {request.UserVoucherStatusId} not found for this seller");
            }

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

            await _context.SaveChangesAsync();

            return MapToVoucherResponse(voucher);
        }

        private async Task<VoucherResponse> DeleteVoucherAsync(int voucherStatusId)
        {
            var voucher = await _context.UserVoucherStatuses
                .Include(v => v.Shop)
                .FirstOrDefaultAsync(v => v.UserVoucherStatusId == voucherStatusId);

            if (voucher == null)
            {
                throw new InvalidOperationException($"Voucher with ID {voucherStatusId} not found");
            }

            var allVouchersWithSameCode = await _context.UserVoucherStatuses
                .Where(v => v.VoucherCode == voucher.VoucherCode && v.ShopId == voucher.ShopId && !v.IsDeleted)
                .ToListAsync();

            foreach (var v in allVouchersWithSameCode)
            {
                v.IsDeleted = true;
                v.Status = "inactive";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted {allVouchersWithSameCode.Count} vouchers with code '{voucher.VoucherCode}'");

            return MapToVoucherResponse(voucher);
        }

        private async Task<VoucherResponse> MapToVoucherResponseWithShop(Entities.UserVoucherStatus voucher)
        {
            var shop = await _context.Sellers.FirstOrDefaultAsync(s => s.SellerId == voucher.ShopId);
            
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
                ShopId = voucher.ShopId,
                ShopName = shop?.ShopName,
                Status = voucher.Status,
                IsDeleted = voucher.IsDeleted
            };
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
                ShopId = voucher.ShopId,
                ShopName = voucher.Shop?.ShopName,
                Status = currentStatus,
                IsDeleted = voucher.IsDeleted
            };
        }

        private string GetCurrentVoucherStatus(Entities.UserVoucherStatus voucher)
        {
            if (voucher.IsDeleted)
                return voucher.Status;

            if (DateTime.Now > voucher.EndDate)
                return "expired";

            if (voucher.RemainingCount.HasValue && voucher.RemainingCount.Value <= 0)
                return "inactive";

            if (voucher.Status == "inactive")
                return "inactive";

            if (DateTime.Now >= voucher.StartDate && DateTime.Now <= voucher.EndDate)
                return "active";

            return "active";
        }

        private string GetGroupStatus(List<Entities.UserVoucherStatus> vouchers)
        {
            if (vouchers.All(v => v.IsDeleted))
                return "deleted";

            if (vouchers.All(v => DateTime.Now > v.EndDate))
                return "expired";

            if (vouchers.All(v => v.RemainingCount.HasValue && v.RemainingCount.Value <= 0))
                return "inactive";

            if (vouchers.Any(v => v.Status == "active" && !v.IsDeleted))
                return "active";

            return "inactive";
        }

        private async Task UpdateVoucherStatusesAsync(List<Entities.UserVoucherStatus> vouchers)
        {
            bool hasChanges = false;

            foreach (var voucher in vouchers)
            {
                if (voucher.IsDeleted) continue;

                var newStatus = GetCurrentVoucherStatus(voucher);

                if (voucher.Status != newStatus)
                {
                    voucher.Status = newStatus;
                    hasChanges = true;
                    _logger.LogInformation($"Auto-updated voucher {voucher.VoucherCode} status from {voucher.Status} to {newStatus}");
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Voucher statuses updated in database");
            }
        }

        public async Task<bool> UseVoucherAsync(int voucherStatusId, int userId)
        {
            try
            {
                var voucher = await _context.UserVoucherStatuses
                    .FirstOrDefaultAsync(v => v.UserVoucherStatusId == voucherStatusId);

                if (voucher == null || voucher.IsDeleted)
                {
                    _logger.LogWarning($"Voucher {voucherStatusId} not found or deleted");
                    return false;
                }

                if (voucher.Status != "active" || DateTime.Now > voucher.EndDate || DateTime.Now < voucher.StartDate)
                {
                    _logger.LogWarning($"Voucher {voucher.VoucherCode} cannot be used - Status: {voucher.Status}, Current time: {DateTime.Now}");
                    return false;
                }

                if (voucher.RemainingCount.HasValue && voucher.RemainingCount.Value <= 0)
                {
                    _logger.LogWarning($"Voucher {voucher.VoucherCode} has no remaining uses");
                    return false;
                }

                voucher.UsageCount = (voucher.UsageCount ?? 0) + 1;
                if (voucher.RemainingCount.HasValue)
                {
                    voucher.RemainingCount = voucher.RemainingCount.Value - 1;

                    if (voucher.RemainingCount.Value <= 0)
                    {
                        voucher.Status = "inactive";
                        _logger.LogInformation($"Voucher {voucher.VoucherCode} automatically set to inactive (used up)");
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Voucher {voucher.VoucherCode} used successfully by user {userId}. Remaining: {voucher.RemainingCount}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error using voucher {voucherStatusId}: {ex.Message}");
                return false;
            }
        }

        public async Task<VoucherResponse?> GetVoucherByCodeAsync(string voucherCode, int? userId = null)
        {
            try
            {
                var voucher = await _context.UserVoucherStatuses
                    .Include(v => v.Shop)
                    .FirstOrDefaultAsync(v => v.VoucherCode == voucherCode && !v.IsDeleted);

                if (voucher == null)
                {
                    _logger.LogWarning($"Voucher with code {voucherCode} not found");
                    return null;
                }

                if (userId.HasValue && voucher.UserInfoId.HasValue && voucher.UserInfoId.Value != userId.Value)
                {
                    _logger.LogWarning($"Voucher {voucherCode} is not for user {userId}");
                    return null;
                }

                return MapToVoucherResponse(voucher);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher by code {voucherCode}: {ex.Message}");
                return null;
            }
        }

        public async Task<VoucherStatsResponse> GetVoucherStatsAsync(string voucherCode, int sellerId)
        {
            try
            {
                var vouchers = await _context.UserVoucherStatuses
                    .Include(v => v.UserInfo)
                    .ThenInclude(ui => ui.User)
                    .Where(v => v.VoucherCode == voucherCode && v.ShopId == sellerId)
                    .ToListAsync();

                if (!vouchers.Any())
                {
                    throw new InvalidOperationException($"Voucher '{voucherCode}' not found for this seller");
                }

                var totalUsers = vouchers.Count;
                var usedCount = vouchers.Sum(v => v.UsageCount ?? 0);
                var remainingCount = vouchers.Sum(v => v.RemainingCount ?? 0);
                var usagePercentage = totalUsers > 0 ? (double)vouchers.Count(v => (v.UsageCount ?? 0) > 0) / totalUsers * 100 : 0;

                var userStats = vouchers.Select(v => new VoucherUserStats
                {
                    UserInfoId = v.UserInfoId ?? 0,
                    UserName = v.UserInfo?.User?.Username,
                    Email = v.UserInfo?.User?.Email,
                    UsageCount = v.UsageCount,
                    RemainingCount = v.RemainingCount,
                    Status = GetCurrentVoucherStatus(v)
                }).ToList();

                return new VoucherStatsResponse
                {
                    VoucherCode = voucherCode,
                    TotalUsers = totalUsers,
                    UsedCount = usedCount,
                    RemainingCount = remainingCount,
                    UsagePercentage = Math.Round(usagePercentage, 2),
                    UserStats = userStats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher stats for {voucherCode}: {ex.Message}");
                throw;
            }
        }
    }
}
