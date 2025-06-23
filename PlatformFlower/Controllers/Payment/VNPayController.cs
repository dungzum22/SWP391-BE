using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Payment;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Payment.VNPay;

namespace PlatformFlower.Controllers.Payment
{
    [ApiController]
    [Route("api/vnpay")]
    public class VNPayController : ControllerBase
    {
        private readonly IVNPayService _vnPayService;
        private readonly IAppLogger _logger;
        private readonly FlowershopContext _context;
        private readonly IConfiguration _configuration;

        public VNPayController(IVNPayService vnPayService, IAppLogger logger, FlowershopContext context, IConfiguration configuration)
        {
            _vnPayService = vnPayService;
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("vnpay_return")]
        public async Task<IActionResult> VNPayReturn([FromQuery] VNPayReturnRequest returnRequest)
        {
            try
            {
                _logger.LogInformation($"VNPay return for order {returnRequest.vnp_TxnRef} - Response: {returnRequest.vnp_ResponseCode}");
                _logger.LogInformation($"VNPay return full data: Amount={returnRequest.vnp_Amount}, BankCode={returnRequest.vnp_BankCode}, TransactionStatus={returnRequest.vnp_TransactionStatus}");

                var result = await _vnPayService.ProcessReturnAsync(returnRequest);

                // Get frontend URL from configuration
                var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:3000";

                if (result == "paid")
                {
                    var redirectUrl = $"{frontendUrl}/payment/success?orderId={returnRequest.vnp_TxnRef}&transactionNo={returnRequest.vnp_TransactionNo}&amount={returnRequest.vnp_Amount}";
                    return Redirect(redirectUrl);
                }
                else if (result == "failed")
                {
                    var redirectUrl = $"{frontendUrl}/payment/failed?orderId={returnRequest.vnp_TxnRef}&responseCode={returnRequest.vnp_ResponseCode}";
                    return Redirect(redirectUrl);
                }
                else
                {
                    var redirectUrl = $"{frontendUrl}/payment/error?orderId={returnRequest.vnp_TxnRef}";
                    return Redirect(redirectUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing VNPay return: {ex.Message}", ex);
                return StatusCode(500, new {
                    status = "error",
                    message = "Internal server error while processing payment",
                    orderId = returnRequest.vnp_TxnRef,
                    error = ex.Message
                });
            }
        }

        // API endpoint for frontend to check payment status
        [HttpGet("payment-status/{orderId}")]
        public async Task<IActionResult> GetPaymentStatus(int orderId)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return NotFound(new {
                        status = "error",
                        message = "Order not found",
                        orderId = orderId
                    });
                }

                return Ok(new {
                    status = "success",
                    orderId = order.OrderId,
                    paymentStatus = order.StatusPayment,
                    totalPrice = order.TotalPrice,
                    createdDate = order.CreatedDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting payment status for order {orderId}: {ex.Message}", ex);
                return StatusCode(500, new {
                    status = "error",
                    message = "Internal server error",
                    orderId = orderId
                });
            }
        }

        // Debug endpoint để test cart clearing
        [HttpPost("debug/clear-cart/{orderId}")]
        public async Task<IActionResult> DebugClearCart(int orderId)
        {
            try
            {
                _logger.LogInformation($"Debug: Attempting to clear cart for order {orderId}");

                // Simulate successful payment processing
                var fakeReturnRequest = new VNPayReturnRequest
                {
                    vnp_TxnRef = orderId.ToString(),
                    vnp_ResponseCode = "00",
                    vnp_TransactionStatus = "00",
                    vnp_Amount = "27000000",
                    vnp_PayDate = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    vnp_TransactionNo = "DEBUG123",
                    vnp_SecureHash = "debug_hash"
                };

                var result = await _vnPayService.ProcessReturnAsync(fakeReturnRequest);

                return Ok(new {
                    status = "debug_success",
                    message = $"Debug cart clearing result: {result}",
                    orderId = orderId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Debug error: {ex.Message}", ex);
                return BadRequest(new {
                    status = "debug_error",
                    message = ex.Message,
                    orderId = orderId
                });
            }
        }
    }
}
