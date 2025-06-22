using PlatformFlower.Models.DTOs.Payment;

namespace PlatformFlower.Services.Payment.VNPay
{
    public interface IVNPayService
    {
        Task<VNPayResponse> CreatePaymentUrlAsync(VNPayRequest request);
        Task<bool> ValidateReturnAsync(VNPayReturnRequest returnRequest);
        Task<string> ProcessReturnAsync(VNPayReturnRequest returnRequest);
    }
}
