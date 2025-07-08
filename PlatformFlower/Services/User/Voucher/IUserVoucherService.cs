using PlatformFlower.Models.DTOs.Voucher;

namespace PlatformFlower.Services.User.Voucher
{
    public interface IUserVoucherService
    {
        Task<List<VoucherResponse>> GetUserVouchersAsync(int userId);
        Task<VoucherResponse?> ValidateVoucherCodeAsync(string voucherCode, int userId);
        Task<VoucherResponse?> GetUserVoucherByIdAsync(int userVoucherStatusId, int userId);
    }
}
