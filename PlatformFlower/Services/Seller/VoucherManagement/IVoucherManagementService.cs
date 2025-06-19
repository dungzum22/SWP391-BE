using PlatformFlower.Models.DTOs.Voucher;

namespace PlatformFlower.Services.Seller.VoucherManagement
{
    public interface IVoucherManagementService
    {
        Task<VoucherResponse> ManageVoucherAsync(CreateVoucherRequest request, int sellerId);
        Task<List<VoucherResponse>> GetAllVouchersAsync(int sellerId);
        Task<VoucherResponse?> GetVoucherByIdAsync(int voucherStatusId, int sellerId);
        Task<bool> UseVoucherAsync(int voucherStatusId, int userId);
        Task<VoucherResponse?> GetVoucherByCodeAsync(string voucherCode, int? userId = null);
        Task<VoucherStatsResponse> GetVoucherStatsAsync(string voucherCode, int sellerId);
    }
}
