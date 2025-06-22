using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Order;
using PlatformFlower.Models.DTOs.Payment;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Payment.VNPay;
using PlatformFlower.Services.User.Cart;

namespace PlatformFlower.Services.User.Order
{
    public class OrderService : IOrderService
    {
        private readonly FlowershopContext _context;
        private readonly IAppLogger _logger;
        private readonly IVNPayService _vnPayService;
        private readonly IConfiguration _configuration;
        private const decimal SHIPPING_FEE = 30000m;

        public OrderService(
            FlowershopContext context,
            IAppLogger logger,
            IVNPayService vnPayService,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _vnPayService = vnPayService;
            _configuration = configuration;
        }

        public async Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation($"Creating order for user {userId}");

                // Get user info and validate address
                var userInfo = await _context.UserInfos
                    .FirstOrDefaultAsync(ui => ui.UserId == userId);

                if (userInfo == null)
                {
                    throw new ArgumentException("User info not found");
                }

                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.AddressId == request.AddressId && a.UserInfoId == userInfo.UserInfoId);

                if (address == null)
                {
                    throw new ArgumentException("Invalid address");
                }

                // Get cart items
                var cartItems = await _context.Carts
                    .Include(c => c.Flower)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    throw new ArgumentException("Cart is empty. Please add items to cart before creating an order.");
                }

                // Validate cart items and calculate subtotal
                decimal subTotal = 0;
                foreach (var cartItem in cartItems)
                {
                    if (cartItem.Flower == null || cartItem.Flower.IsDeleted || cartItem.Flower.Status != "active")
                    {
                        throw new ArgumentException($"Flower in cart is no longer available");
                    }

                    if (cartItem.Flower.AvailableQuantity < cartItem.Quantity)
                    {
                        throw new ArgumentException($"Insufficient stock for flower {cartItem.Flower.FlowerName}. Available: {cartItem.Flower.AvailableQuantity}, Requested: {cartItem.Quantity}");
                    }

                    subTotal += cartItem.UnitPrice * cartItem.Quantity;
                }

                decimal voucherDiscountAmount = 0;
                string? voucherCode = null;
                double? voucherDiscount = null;

                if (request.UserVoucherStatusId.HasValue)
                {
                    var voucher = await _context.UserVoucherStatuses
                        .FirstOrDefaultAsync(v => v.UserVoucherStatusId == request.UserVoucherStatusId.Value);

                    if (voucher != null && voucher.Status == "active" && !voucher.IsDeleted)
                    {
                        voucherDiscountAmount = (decimal)(subTotal * (decimal)voucher.Discount / 100);
                        voucherCode = voucher.VoucherCode;
                        voucherDiscount = voucher.Discount;
                    }
                }

                var totalPrice = subTotal + SHIPPING_FEE - voucherDiscountAmount;

                var order = new Entities.Order
                {
                    UserId = userId,
                    PhoneNumber = request.PhoneNumber,
                    PaymentMethod = request.PaymentMethod,
                    DeliveryMethod = request.DeliveryMethod,
                    CreatedDate = DateTime.Now,
                    UserVoucherStatusId = request.UserVoucherStatusId,
                    AddressId = request.AddressId,
                    StatusPayment = "pending",
                    TotalPrice = totalPrice
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order details from cart items
                foreach (var cartItem in cartItems)
                {
                    var orderDetail = new Entities.OrdersDetail
                    {
                        OrderId = order.OrderId,
                        FlowerId = cartItem.FlowerId,
                        Price = cartItem.UnitPrice,
                        Amount = cartItem.Quantity,
                        UserVoucherStatusId = request.UserVoucherStatusId,
                        Status = "pending",
                        CreatedAt = DateTime.Now,
                        AddressId = request.AddressId,
                        DeliveryMethod = request.DeliveryMethod
                    };

                    _context.OrdersDetails.Add(orderDetail);

                    // Update flower inventory
                    if (cartItem.Flower != null)
                    {
                        cartItem.Flower.AvailableQuantity -= cartItem.Quantity;
                    }
                }

                await _context.SaveChangesAsync();

                // Handle voucher usage
                if (request.UserVoucherStatusId.HasValue)
                {
                    var voucherToUse = await _context.UserVoucherStatuses
                        .FirstOrDefaultAsync(v => v.UserVoucherStatusId == request.UserVoucherStatusId.Value);

                    if (voucherToUse != null && voucherToUse.Status == "active" && !voucherToUse.IsDeleted)
                    {
                        voucherToUse.UsageCount = (voucherToUse.UsageCount ?? 0) + 1;
                        if (voucherToUse.RemainingCount.HasValue)
                        {
                            voucherToUse.RemainingCount = voucherToUse.RemainingCount.Value - 1;

                            if (voucherToUse.RemainingCount.Value <= 0)
                            {
                                voucherToUse.Status = "inactive";
                            }
                        }
                    }
                }

                // NOTE: Cart is NOT cleared here - it will be cleared after successful payment confirmation

                var vnpayRequest = new VNPayRequest
                {
                    OrderId = order.OrderId,
                    Amount = totalPrice,
                    OrderInfo = $"Thanh toan don hang #{order.OrderId}",
                    ReturnUrl = _configuration.GetSection("VNPay")["ReturnUrl"]!
                };

                var vnpayResponse = await _vnPayService.CreatePaymentUrlAsync(vnpayRequest);

                await transaction.CommitAsync();

                var response = await MapToOrderResponse(order);
                response.PaymentUrl = vnpayResponse.PaymentUrl;

                _logger.LogInformation($"Order {order.OrderId} created successfully for user {userId}");

                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error creating order for user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<OrderResponse?> GetOrderByIdAsync(int orderId, int userId)
        {
            var order = await _context.Orders
                .Include(o => o.Address)
                .Include(o => o.OrdersDetails)
                    .ThenInclude(od => od.Flower)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            return order != null ? await MapToOrderResponse(order) : null;
        }

        public async Task<List<OrderResponse>> GetUserOrdersAsync(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.Address)
                .Include(o => o.OrdersDetails)
                    .ThenInclude(od => od.Flower)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            var result = new List<OrderResponse>();
            foreach (var order in orders)
            {
                result.Add(await MapToOrderResponse(order));
            }

            return result;
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null) return false;

                order.StatusPayment = status;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Order {orderId} status updated to {status}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order {orderId} status: {ex.Message}", ex);
                return false;
            }
        }

        private async Task<OrderResponse> MapToOrderResponse(Entities.Order order)
        {
            var voucher = order.UserVoucherStatusId.HasValue
                ? await _context.UserVoucherStatuses
                    .FirstOrDefaultAsync(v => v.UserVoucherStatusId == order.UserVoucherStatusId.Value)
                : null;

            var subTotal = order.OrdersDetails?.Sum(od => od.Price * od.Amount) ?? 0;
            var voucherDiscountAmount = voucher != null ? (decimal)(subTotal * (decimal)voucher.Discount / 100) : 0;

            return new OrderResponse
            {
                OrderId = order.OrderId,
                UserId = order.UserId ?? 0,
                PhoneNumber = order.PhoneNumber ?? string.Empty,
                PaymentMethod = order.PaymentMethod,
                DeliveryMethod = order.DeliveryMethod,
                CreatedDate = order.CreatedDate ?? DateTime.Now,
                UserVoucherStatusId = order.UserVoucherStatusId,
                VoucherCode = voucher?.VoucherCode,
                VoucherDiscount = voucher?.Discount,
                AddressId = order.AddressId ?? 0,
                AddressDescription = order.Address?.Description,
                StatusPayment = order.StatusPayment ?? "pending",
                SubTotal = subTotal,
                ShippingFee = SHIPPING_FEE,
                VoucherDiscountAmount = voucherDiscountAmount,
                TotalPrice = order.TotalPrice ?? 0,
                Items = order.OrdersDetails?.Select(od => new OrderItemResponse
                {
                    OrderDetailId = od.OrderDetailId,
                    FlowerId = od.FlowerId ?? 0,
                    FlowerName = od.Flower?.FlowerName ?? string.Empty,
                    FlowerImage = od.Flower?.ImageUrl,
                    UnitPrice = od.Price,
                    Quantity = od.Amount,
                    TotalPrice = od.Price * od.Amount,
                    Status = od.Status ?? "pending"
                }).ToList() ?? new List<OrderItemResponse>()
            };
        }
    }
}
