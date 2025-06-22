using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Voucher;

namespace PlatformFlower.Services.Admin.VoucherManagement
{
    public static class AdminVoucherValidation
    {
        public static async Task ValidateCreateVoucherAsync(CreateVoucherRequest request, FlowershopContext context)
        {
            ValidateVoucherCode(request.VoucherCode);
            ValidateDiscount(request.Discount);
            ValidateDateRange(request.StartDate, request.EndDate);
            ValidateUsageLimits(request.UsageLimit, request.RemainingCount);
            await ValidateVoucherCodeUniqueness(request.VoucherCode!, context);
        }

        public static async Task ValidateUpdateVoucherAsync(CreateVoucherRequest request, FlowershopContext context)
        {
            if (request.UserVoucherStatusId == null || request.UserVoucherStatusId <= 0)
            {
                throw new ArgumentException("Valid UserVoucherStatusId is required for update");
            }

            await ValidateVoucherExists(request.UserVoucherStatusId.Value, context);

            if (!string.IsNullOrEmpty(request.VoucherCode))
            {
                ValidateVoucherCode(request.VoucherCode);
                await ValidateVoucherCodeUniquenessForUpdate(request.VoucherCode, request.UserVoucherStatusId.Value, context);
            }

            if (request.Discount.HasValue)
            {
                ValidateDiscount(request.Discount);
            }

            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                ValidateDateRange(request.StartDate, request.EndDate);
            }

            if (request.UsageLimit.HasValue || request.RemainingCount.HasValue)
            {
                ValidateUsageLimits(request.UsageLimit, request.RemainingCount);
            }
        }

        public static async Task ValidateDeleteVoucherAsync(int voucherStatusId, FlowershopContext context)
        {
            var voucher = await context.UserVoucherStatuses
                .FirstOrDefaultAsync(v => v.UserVoucherStatusId == voucherStatusId);

            if (voucher == null)
            {
                throw new InvalidOperationException($"Voucher with ID {voucherStatusId} not found");
            }

            var hasActiveUsage = await context.Orders
                .AnyAsync(o => o.UserVoucherStatusId == voucherStatusId && o.StatusPayment != "cancelled");

            if (hasActiveUsage)
            {
                throw new InvalidOperationException("Cannot delete voucher that is being used in active orders");
            }
        }

        private static void ValidateVoucherCode(string? voucherCode)
        {
            if (string.IsNullOrWhiteSpace(voucherCode))
            {
                throw new ArgumentException("Voucher code is required");
            }

            if (voucherCode.Length < 3 || voucherCode.Length > 50)
            {
                throw new ArgumentException("Voucher code must be between 3 and 50 characters");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(voucherCode, @"^[A-Za-z0-9_-]+$"))
            {
                throw new ArgumentException("Voucher code can only contain letters, numbers, hyphens, and underscores");
            }
        }

        private static void ValidateDiscount(double? discount)
        {
            if (!discount.HasValue)
            {
                throw new ArgumentException("Discount is required");
            }

            if (discount.Value <= 0 || discount.Value > 100)
            {
                throw new ArgumentException("Discount must be between 0 and 100 percent");
            }
        }

        private static void ValidateDateRange(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                throw new ArgumentException("Both start date and end date are required");
            }

            if (startDate.Value >= endDate.Value)
            {
                throw new ArgumentException("Start date must be before end date");
            }

            if (endDate.Value <= DateTime.Now.AddHours(-1))
            {
                throw new ArgumentException("End date must be in the future");
            }
        }

        private static void ValidateUsageLimits(int? usageLimit, int? remainingCount)
        {
            if (usageLimit.HasValue && usageLimit.Value <= 0)
            {
                throw new ArgumentException("Usage limit must be greater than 0");
            }

            if (remainingCount.HasValue && remainingCount.Value < 0)
            {
                throw new ArgumentException("Remaining count cannot be negative");
            }

            if (usageLimit.HasValue && remainingCount.HasValue && remainingCount.Value > usageLimit.Value)
            {
                throw new ArgumentException("Remaining count cannot be greater than usage limit");
            }
        }

        private static async Task ValidateVoucherCodeUniqueness(string voucherCode, FlowershopContext context)
        {
            var existingVoucher = await context.UserVoucherStatuses
                .AnyAsync(v => v.VoucherCode == voucherCode && !v.IsDeleted);

            if (existingVoucher)
            {
                throw new InvalidOperationException($"Voucher code '{voucherCode}' already exists");
            }
        }

        private static async Task ValidateVoucherCodeUniquenessForUpdate(string voucherCode, int currentVoucherId, FlowershopContext context)
        {
            var existingVoucher = await context.UserVoucherStatuses
                .AnyAsync(v => v.VoucherCode == voucherCode && v.UserVoucherStatusId != currentVoucherId && !v.IsDeleted);

            if (existingVoucher)
            {
                throw new InvalidOperationException($"Voucher code '{voucherCode}' already exists");
            }
        }

        private static async Task ValidateVoucherExists(int voucherStatusId, FlowershopContext context)
        {
            var exists = await context.UserVoucherStatuses
                .AnyAsync(v => v.UserVoucherStatusId == voucherStatusId);

            if (!exists)
            {
                throw new InvalidOperationException($"Voucher with ID {voucherStatusId} not found");
            }
        }
    }
}
