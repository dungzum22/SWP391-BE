using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Voucher;

namespace PlatformFlower.Services.Seller.VoucherManagement
{
    public static class VoucherValidation
    {
        public static async Task ValidateCreateVoucherAsync(CreateVoucherRequest request, FlowershopContext context, int sellerId)
        {
            if (string.IsNullOrWhiteSpace(request.VoucherCode))
            {
                throw new ArgumentException("Voucher code is required");
            }

            if (request.VoucherCode.Length > 50)
            {
                throw new ArgumentException("Voucher code cannot exceed 50 characters");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(request.VoucherCode, @"^[A-Za-z0-9\-_]+$"))
            {
                throw new ArgumentException("Voucher code can only contain letters, numbers, hyphens, and underscores");
            }

            if (!request.Discount.HasValue || request.Discount.Value <= 0)
            {
                throw new ArgumentException("Discount must be greater than 0");
            }

            if (request.Discount.Value > 1 && request.Discount.Value > 10000000)
            {
                throw new ArgumentException("Discount amount is too large (max 10M VND)");
            }

            if (!request.StartDate.HasValue)
            {
                throw new ArgumentException("Start date is required");
            }

            if (!request.EndDate.HasValue)
            {
                throw new ArgumentException("End date is required");
            }

            if (request.EndDate.Value <= request.StartDate.Value)
            {
                throw new ArgumentException("End date must be after start date");
            }

            if (request.StartDate.Value < DateTime.Now.Date)
            {
                throw new ArgumentException("Start date cannot be in the past");
            }

            if (request.UsageLimit.HasValue && request.UsageLimit.Value <= 0)
            {
                throw new ArgumentException("Usage limit must be greater than 0");
            }

            if (request.RemainingCount.HasValue && request.RemainingCount.Value < 0)
            {
                throw new ArgumentException("Remaining count cannot be negative");
            }

            var existingVoucher = await context.UserVoucherStatuses
                .FirstOrDefaultAsync(v => v.VoucherCode == request.VoucherCode && v.ShopId == sellerId && !v.IsDeleted);

            if (existingVoucher != null)
            {
                throw new InvalidOperationException($"Voucher code '{request.VoucherCode}' already exists for this shop");
            }

            var seller = await context.Sellers.FirstOrDefaultAsync(s => s.SellerId == sellerId);
            if (seller == null)
            {
                throw new InvalidOperationException($"Seller with ID {sellerId} not found");
            }
        }

        public static async Task ValidateUpdateVoucherAsync(CreateVoucherRequest request, FlowershopContext context, int sellerId)
        {
            if (!request.UserVoucherStatusId.HasValue || request.UserVoucherStatusId.Value <= 0)
            {
                throw new ArgumentException("Voucher ID is required for update");
            }

            var existingVoucher = await context.UserVoucherStatuses
                .FirstOrDefaultAsync(v => v.UserVoucherStatusId == request.UserVoucherStatusId.Value);

            if (existingVoucher == null)
            {
                throw new InvalidOperationException($"Voucher with ID {request.UserVoucherStatusId} not found");
            }

            if (existingVoucher.ShopId != sellerId)
            {
                throw new UnauthorizedAccessException("You can only update your own vouchers");
            }

            if (!string.IsNullOrEmpty(request.VoucherCode))
            {
                if (request.VoucherCode.Length > 50)
                {
                    throw new ArgumentException("Voucher code cannot exceed 50 characters");
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(request.VoucherCode, @"^[A-Za-z0-9\-_]+$"))
                {
                    throw new ArgumentException("Voucher code can only contain letters, numbers, hyphens, and underscores");
                }

                var conflictingVoucher = await context.UserVoucherStatuses
                    .FirstOrDefaultAsync(v => v.VoucherCode == request.VoucherCode
                                            && v.ShopId == sellerId
                                            && v.UserVoucherStatusId != request.UserVoucherStatusId
                                            && !v.IsDeleted);

                if (conflictingVoucher != null)
                {
                    throw new InvalidOperationException($"Voucher code '{request.VoucherCode}' already exists for this shop");
                }
            }

            if (request.Discount.HasValue && request.Discount.Value <= 0)
            {
                throw new ArgumentException("Discount must be greater than 0");
            }

            if (request.Discount.HasValue && request.Discount.Value > 1 && request.Discount.Value > 10000000)
            {
                throw new ArgumentException("Discount amount is too large (max 10M VND)");
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate.Value <= request.StartDate.Value)
            {
                throw new ArgumentException("End date must be after start date");
            }

            if (request.UsageLimit.HasValue && request.UsageLimit.Value <= 0)
            {
                throw new ArgumentException("Usage limit must be greater than 0");
            }

            if (request.RemainingCount.HasValue && request.RemainingCount.Value < 0)
            {
                throw new ArgumentException("Remaining count cannot be negative");
            }
        }

        public static async Task ValidateDeleteVoucherAsync(int voucherStatusId, FlowershopContext context, int sellerId)
        {
            var voucher = await context.UserVoucherStatuses
                .FirstOrDefaultAsync(v => v.UserVoucherStatusId == voucherStatusId);

            if (voucher == null)
            {
                throw new InvalidOperationException($"Voucher with ID {voucherStatusId} not found");
            }

            if (voucher.ShopId != sellerId)
            {
                throw new UnauthorizedAccessException("You can only delete your own vouchers");
            }

            var hasActiveUsage = await context.Orders
                .AnyAsync(o => o.UserVoucherStatusId == voucherStatusId && o.StatusPayment != "cancelled");

            if (hasActiveUsage)
            {
                throw new InvalidOperationException("Cannot delete voucher that is being used in active orders");
            }
        }
    }
}
