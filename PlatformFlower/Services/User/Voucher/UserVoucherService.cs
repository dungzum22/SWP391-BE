using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Voucher;
using PlatformFlower.Services.Common.Logging;

namespace PlatformFlower.Services.User.Voucher
{
    public class UserVoucherService : IUserVoucherService
    {
        private readonly FlowershopContext _context;
        private readonly IAppLogger _logger;

        public UserVoucherService(
            FlowershopContext context,
            IAppLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<VoucherResponse>> GetUserVouchersAsync(int userId)
        {
            try
            {
                _logger.LogInformation($"Getting vouchers for user {userId}");

                var vouchers = await _context.UserVoucherStatuses
                    .Where(v => v.UserInfoId == userId && !v.IsDeleted)
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync();

                var result = vouchers.Select(MapToVoucherResponse).ToList();

                _logger.LogInformation($"Retrieved {result.Count} vouchers for user {userId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting vouchers for user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<VoucherResponse?> ValidateVoucherCodeAsync(string voucherCode, int userId)
        {
            try
            {
                _logger.LogInformation($"Validating voucher code '{voucherCode}' for user {userId}");

                var voucher = await _context.UserVoucherStatuses
                    .FirstOrDefaultAsync(v => v.VoucherCode == voucherCode 
                                            && v.UserInfoId == userId 
                                            && !v.IsDeleted);

                if (voucher == null)
                {
                    _logger.LogWarning($"Voucher code '{voucherCode}' not found for user {userId}");
                    return null;
                }

                var result = MapToVoucherResponse(voucher);
                
                _logger.LogInformation($"Voucher validation result for '{voucherCode}': IsActive={result.IsActive}, IsExpired={result.IsExpired}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating voucher code '{voucherCode}' for user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<VoucherResponse?> GetUserVoucherByIdAsync(int userVoucherStatusId, int userId)
        {
            try
            {
                _logger.LogInformation($"Getting voucher {userVoucherStatusId} for user {userId}");

                var voucher = await _context.UserVoucherStatuses
                    .FirstOrDefaultAsync(v => v.UserVoucherStatusId == userVoucherStatusId 
                                            && v.UserInfoId == userId 
                                            && !v.IsDeleted);

                if (voucher == null)
                {
                    _logger.LogWarning($"Voucher {userVoucherStatusId} not found for user {userId}");
                    return null;
                }

                var result = MapToVoucherResponse(voucher);
                _logger.LogInformation($"Successfully retrieved voucher {userVoucherStatusId} for user {userId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher {userVoucherStatusId} for user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        private static VoucherResponse MapToVoucherResponse(Entities.UserVoucherStatus voucher)
        {
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
                Status = voucher.Status,
                IsDeleted = voucher.IsDeleted
            };
        }
    }
}
