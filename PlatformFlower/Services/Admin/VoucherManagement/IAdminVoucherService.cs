using PlatformFlower.Models.DTOs.Voucher;

namespace PlatformFlower.Services.Admin.VoucherManagement
{
    public interface IAdminVoucherService
    {
        Task<VoucherResponse> ManageVoucherAsync(CreateVoucherRequest request);
        Task<List<VoucherResponse>> GetAllVouchersAsync();
        Task<VoucherResponse?> GetVoucherByIdAsync(int voucherStatusId);
        Task<bool> UseVoucherAsync(int voucherStatusId, int userId);
        Task<VoucherResponse?> GetVoucherByCodeAsync(string voucherCode, int? userId = null);
        Task<VoucherStatsResponse> GetVoucherStatsAsync(string voucherCode);
    }
}
