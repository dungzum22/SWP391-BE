using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Payment;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.User.Cart;
using System.Security.Cryptography;
using System.Text;
using System.Net;

namespace PlatformFlower.Services.Payment.VNPay
{
    public class VNPayService : IVNPayService
    {
        private readonly IConfiguration _configuration;
        private readonly FlowershopContext _context;
        private readonly IAppLogger _logger;
        private readonly ICartService _cartService;

        public VNPayService(IConfiguration configuration, FlowershopContext context, IAppLogger logger, ICartService cartService)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _cartService = cartService;
        }

        public async Task<VNPayResponse> CreatePaymentUrlAsync(VNPayRequest request)
        {
            try
            {
                var vnpayConfig = _configuration.GetSection("VNPay");
                var apiUrl = vnpayConfig["ApiUrl"];
                var tmnCode = vnpayConfig["TmnCode"];
                var hashSecret = vnpayConfig["HashSecret"];

                var vnpParams = new SortedList<string, string>
                {
                    {"vnp_Version", "2.1.0"},
                    {"vnp_Command", "pay"},
                    {"vnp_TmnCode", tmnCode},
                    {"vnp_Amount", ((long)(request.Amount * 100)).ToString()},
                    {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")},
                    {"vnp_CurrCode", "VND"},
                    {"vnp_IpAddr", "127.0.0.1"},
                    {"vnp_Locale", "vn"},
                    {"vnp_OrderInfo", request.OrderInfo},
                    {"vnp_OrderType", "other"},
                    {"vnp_ReturnUrl", request.ReturnUrl},
                    {"vnp_TxnRef", request.OrderId.ToString()}
                };

                var hashData = new StringBuilder();
                var query = new StringBuilder();

                foreach (var param in vnpParams)
                {
                    if (!string.IsNullOrEmpty(param.Value))
                    {
                        hashData.Append(WebUtility.UrlEncode(param.Key));
                        hashData.Append("=");
                        hashData.Append(WebUtility.UrlEncode(param.Value));
                        hashData.Append("&");

                        query.Append(WebUtility.UrlEncode(param.Key));
                        query.Append("=");
                        query.Append(WebUtility.UrlEncode(param.Value));
                        query.Append("&");
                    }
                }

                if (hashData.Length > 0)
                {
                    hashData.Length -= 1;
                }

                var vnpSecureHash = HmacSHA512(hashSecret, hashData.ToString());
                query.Append("vnp_SecureHash=");
                query.Append(vnpSecureHash);

                var paymentUrl = $"{apiUrl}?{query}";

                _logger.LogInformation($"VNPay payment URL created for order {request.OrderId}");

                return new VNPayResponse
                {
                    Success = true,
                    PaymentUrl = paymentUrl,
                    Message = "Payment URL created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating VNPay payment URL: {ex.Message}", ex);
                return new VNPayResponse
                {
                    Success = false,
                    PaymentUrl = string.Empty,
                    Message = "Failed to create payment URL"
                };
            }
        }

        public async Task<bool> ValidateReturnAsync(VNPayReturnRequest returnRequest)
        {
            try
            {
                var vnpayConfig = _configuration.GetSection("VNPay");
                var hashSecret = vnpayConfig["HashSecret"];

                var vnpParams = new SortedList<string, string>
                {
                    {"vnp_Amount", returnRequest.vnp_Amount},
                    {"vnp_BankCode", returnRequest.vnp_BankCode},
                    {"vnp_BankTranNo", returnRequest.vnp_BankTranNo},
                    {"vnp_CardType", returnRequest.vnp_CardType},
                    {"vnp_OrderInfo", returnRequest.vnp_OrderInfo},
                    {"vnp_PayDate", returnRequest.vnp_PayDate},
                    {"vnp_ResponseCode", returnRequest.vnp_ResponseCode},
                    {"vnp_TmnCode", returnRequest.vnp_TmnCode},
                    {"vnp_TransactionNo", returnRequest.vnp_TransactionNo},
                    {"vnp_TransactionStatus", returnRequest.vnp_TransactionStatus},
                    {"vnp_TxnRef", returnRequest.vnp_TxnRef}
                };

                var hashData = new StringBuilder();
                foreach (var param in vnpParams)
                {
                    if (!string.IsNullOrEmpty(param.Value))
                    {
                        hashData.Append(WebUtility.UrlEncode(param.Key));
                        hashData.Append("=");
                        hashData.Append(WebUtility.UrlEncode(param.Value));
                        hashData.Append("&");
                    }
                }

                if (hashData.Length > 0)
                {
                    hashData.Length -= 1;
                }

                var computedHash = HmacSHA512(hashSecret, hashData.ToString());
                var isValidSignature = computedHash.Equals(returnRequest.vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);

                _logger.LogInformation($"VNPay signature validation for order {returnRequest.vnp_TxnRef}: {isValidSignature}");

                return isValidSignature;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating VNPay return: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<string> ProcessReturnAsync(VNPayReturnRequest returnRequest)
        {
            try
            {
                var isValid = await ValidateReturnAsync(returnRequest);
                if (!isValid)
                {
                    _logger.LogWarning($"Invalid VNPay signature for order {returnRequest.vnp_TxnRef}");
                    return "Invalid signature";
                }

                var orderId = int.Parse(returnRequest.vnp_TxnRef);
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    _logger.LogWarning($"Order {orderId} not found");
                    return "Order not found";
                }

                _logger.LogInformation($"VNPay return processing - ResponseCode: {returnRequest.vnp_ResponseCode}, TransactionStatus: {returnRequest.vnp_TransactionStatus}");

                if (returnRequest.vnp_ResponseCode == "00" && returnRequest.vnp_TransactionStatus == "00")
                {
                    order.StatusPayment = "paid";
                    _logger.LogInformation($"Order {orderId} payment successful");

                    // Clear cart only after successful payment
                    if (order.UserId.HasValue)
                    {
                        _logger.LogInformation($"Attempting to clear cart for user {order.UserId.Value}");
                        await _cartService.ClearCartAsync(order.UserId.Value);
                        _logger.LogInformation($"Cart cleared successfully for user {order.UserId.Value} after successful payment for order {orderId}");
                    }
                    else
                    {
                        _logger.LogWarning($"Order {orderId} has no UserId - cannot clear cart");
                    }
                }
                else
                {
                    order.StatusPayment = "failed";
                    _logger.LogWarning($"Order {orderId} payment failed - Response: {returnRequest.vnp_ResponseCode}. Cart remains intact for retry.");
                }

                await _context.SaveChangesAsync();
                return order.StatusPayment;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing VNPay return: {ex.Message}", ex);
                return "Error processing payment";
            }
        }

        private string HmacSHA512(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(dataBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
