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

                // Validate shipping fee (must be positive)
                if (request.ShippingFee < 0)
                {
                    throw new ArgumentException("Shipping fee must be positive");
                }

                var totalPrice = subTotal + request.ShippingFee - voucherDiscountAmount;

                var order = new Entities.Order
                {
                    UserId = userId,
                    PhoneNumber = request.PhoneNumber,
                    PaymentMethod = request.PaymentMethod,
                    DeliveryMethod = request.DeliveryMethod,
                    CreatedDate = DateTime.Now,
                    UserVoucherStatusId = request.UserVoucherStatusId,
                    AddressId = request.AddressId,
                    Status = "pending",
                    StatusPayment = "pending",
                    ShippingFee = request.ShippingFee,
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

                await transaction.CommitAsync();

                var response = await MapToOrderResponse(order);

                // Only create VNPay URL if payment method is VNPay
                if (request.PaymentMethod.ToLower() == "vnpay")
                {
                    var vnpayRequest = new VNPayRequest
                    {
                        OrderId = order.OrderId,
                        Amount = totalPrice,
                        OrderInfo = $"Thanh toan don hang #{order.OrderId}",
                        ReturnUrl = _configuration.GetSection("VNPay")["ReturnUrl"]!
                    };

                    var vnpayResponse = await _vnPayService.CreatePaymentUrlAsync(vnpayRequest);
                    response.PaymentUrl = vnpayResponse.PaymentUrl;
                }
                else
                {
                    // For COD, no payment URL needed
                    response.PaymentUrl = null;
                }

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
                ShippingFee = order.ShippingFee ?? 30000m,
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

        // Admin methods implementation
        public async Task<List<AdminOrderResponse>> GetAllOrdersAsync(AdminOrderListRequest request)
        {
            try
            {
                _logger.LogInformation($"Admin getting orders with filters - Status: {request.Status}, Page: {request.Page}");

                var query = _context.Orders
                    .Include(o => o.User)
                        .ThenInclude(u => u.UserInfos)
                    .Include(o => o.Address)
                    .Include(o => o.OrdersDetails)
                        .ThenInclude(od => od.Flower)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(o => o.StatusPayment == request.Status);
                }

                if (request.StartDate.HasValue)
                {
                    query = query.Where(o => o.CreatedDate >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(o => o.CreatedDate <= request.EndDate.Value);
                }

                if (request.UserId.HasValue)
                {
                    query = query.Where(o => o.UserId == request.UserId.Value);
                }

                if (!string.IsNullOrEmpty(request.PaymentMethod))
                {
                    query = query.Where(o => o.PaymentMethod == request.PaymentMethod);
                }

                if (!string.IsNullOrEmpty(request.DeliveryMethod))
                {
                    query = query.Where(o => o.DeliveryMethod == request.DeliveryMethod);
                }

                // Apply sorting
                query = request.SortBy?.ToLower() switch
                {
                    "orderid" => request.SortOrder?.ToLower() == "asc"
                        ? query.OrderBy(o => o.OrderId)
                        : query.OrderByDescending(o => o.OrderId),
                    "totalprice" => request.SortOrder?.ToLower() == "asc"
                        ? query.OrderBy(o => o.TotalPrice)
                        : query.OrderByDescending(o => o.TotalPrice),
                    "status" => request.SortOrder?.ToLower() == "asc"
                        ? query.OrderBy(o => o.StatusPayment)
                        : query.OrderByDescending(o => o.StatusPayment),
                    _ => request.SortOrder?.ToLower() == "asc"
                        ? query.OrderBy(o => o.CreatedDate)
                        : query.OrderByDescending(o => o.CreatedDate)
                };

                // Apply pagination
                var skip = (request.Page - 1) * request.PageSize;
                var orders = await query
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToListAsync();

                var result = new List<AdminOrderResponse>();
                foreach (var order in orders)
                {
                    result.Add(await MapToAdminOrderResponse(order));
                }

                _logger.LogInformation($"Retrieved {result.Count} orders for admin");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting orders for admin: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<AdminOrderDetailResponse>> GetAllOrdersWithDetailsAsync()
        {
            try
            {
                _logger.LogInformation("Admin getting all orders with details");

                var orders = await _context.Orders
                    .Include(o => o.User)
                        .ThenInclude(u => u.UserInfos)
                    .Include(o => o.Address)
                    .Include(o => o.OrdersDetails)
                        .ThenInclude(od => od.Flower)
                            .ThenInclude(f => f.Category)
                    .OrderByDescending(o => o.CreatedDate)
                    .ToListAsync();

                var result = new List<AdminOrderDetailResponse>();
                foreach (var order in orders)
                {
                    result.Add(await MapToAdminOrderDetailResponse(order));
                }

                _logger.LogInformation($"Retrieved {result.Count} orders with details for admin");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all orders with details for admin: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<AdminOrderDetailResponse?> GetOrderDetailsByIdAsync(int orderId)
        {
            try
            {
                _logger.LogInformation($"Admin getting order details for ID: {orderId}");

                var order = await _context.Orders
                    .Include(o => o.User)
                        .ThenInclude(u => u.UserInfos)
                    .Include(o => o.Address)
                    .Include(o => o.OrdersDetails)
                        .ThenInclude(od => od.Flower)
                            .ThenInclude(f => f.Category)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    _logger.LogWarning($"Order not found with ID: {orderId}");
                    return null;
                }

                var result = await MapToAdminOrderDetailResponse(order);
                _logger.LogInformation($"Retrieved order details for ID: {orderId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order details for ID {orderId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<AdminOrderDetailResponse?> UpdateOrderStatusAdminAsync(int orderId, UpdateOrderStatusRequest request)
        {
            try
            {
                _logger.LogInformation($"Admin updating order {orderId} status to {request.Status}");

                var order = await _context.Orders
                    .Include(o => o.User)
                        .ThenInclude(u => u.UserInfos)
                    .Include(o => o.Address)
                    .Include(o => o.OrdersDetails)
                        .ThenInclude(od => od.Flower)
                            .ThenInclude(f => f.Category)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    _logger.LogWarning($"Order not found with ID: {orderId}");
                    return null;
                }

                // Get current status for logging
                var oldOrderStatus = order.Status;
                var oldPaymentStatus = order.StatusPayment;

                if (!order.OrdersDetails.Any())
                {
                    _logger.LogWarning($"No order details found for order {orderId}");
                    return null;
                }

                // Update order status
                order.Status = request.Status;

                // Update all order details with the new status
                foreach (var detail in order.OrdersDetails)
                {
                    detail.Status = request.Status;
                }

                // If order is delivered, also update payment status to completed
                // If order is canceled, keep payment status as is (might be paid already)
                if (request.Status.ToLower() == "delivered")
                {
                    order.StatusPayment = "completed";
                }
                else if (request.Status.ToLower() == "canceled" && order.StatusPayment == "pending")
                {
                    order.StatusPayment = "canceled";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Order {orderId} status updated from '{oldOrderStatus}' to '{request.Status}'. " +
                                     $"Payment status: '{oldPaymentStatus}' to '{order.StatusPayment}'. " +
                                     $"Updated {order.OrdersDetails.Count} order details.");

                // Return the updated order with details
                return await MapToAdminOrderDetailResponse(order);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order {orderId} status: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<OrderStatisticsResponse> GetOrderStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                _logger.LogInformation($"Admin getting order statistics from {startDate} to {endDate}");

                var query = _context.Orders.AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(o => o.CreatedDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(o => o.CreatedDate <= endDate.Value);
                }

                var orders = await query
                    .Include(o => o.User)
                        .ThenInclude(u => u.UserInfos)
                    .ToListAsync();

                var totalOrders = orders.Count;
                var totalRevenue = orders.Sum(o => o.TotalPrice ?? 0);
                var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

                var statusCounts = orders.GroupBy(o => o.StatusPayment ?? "pending")
                    .ToDictionary(g => g.Key, g => g.Count());

                var dailyStats = orders
                    .GroupBy(o => o.CreatedDate?.Date ?? DateTime.Today)
                    .Select(g => new DailyOrderStats
                    {
                        Date = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.TotalPrice ?? 0)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                var paymentMethodStats = orders
                    .GroupBy(o => o.PaymentMethod)
                    .Select(g => new PaymentMethodStats
                    {
                        PaymentMethod = g.Key,
                        OrderCount = g.Count(),
                        TotalAmount = g.Sum(o => o.TotalPrice ?? 0),
                        Percentage = totalRevenue > 0 ? (g.Sum(o => o.TotalPrice ?? 0) / totalRevenue) * 100 : 0
                    })
                    .OrderByDescending(p => p.TotalAmount)
                    .ToList();

                var topCustomers = orders
                    .GroupBy(o => new { o.UserId, o.User?.Username })
                    .Select(g => new TopCustomerStats
                    {
                        UserId = g.Key.UserId ?? 0,
                        Username = g.Key.Username ?? "Unknown",
                        CustomerName = g.First().User?.UserInfos.FirstOrDefault()?.FullName,
                        OrderCount = g.Count(),
                        TotalSpent = g.Sum(o => o.TotalPrice ?? 0)
                    })
                    .OrderByDescending(c => c.TotalSpent)
                    .Take(10)
                    .ToList();

                var result = new OrderStatisticsResponse
                {
                    TotalOrders = totalOrders,
                    TotalRevenue = totalRevenue,
                    PendingOrders = statusCounts.GetValueOrDefault("pending", 0),
                    AcceptedOrders = statusCounts.GetValueOrDefault("accepted", 0),
                    PendingDeliveryOrders = statusCounts.GetValueOrDefault("pending delivery", 0),
                    DeliveredOrders = statusCounts.GetValueOrDefault("delivered", 0),
                    CanceledOrders = statusCounts.GetValueOrDefault("canceled", 0),
                    AverageOrderValue = averageOrderValue,
                    DailyStats = dailyStats,
                    PaymentMethodStats = paymentMethodStats,
                    TopCustomers = topCustomers
                };

                _logger.LogInformation($"Generated order statistics: {totalOrders} orders, {totalRevenue:C} revenue");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating order statistics: {ex.Message}", ex);
                throw;
            }
        }

        private async Task<AdminOrderResponse> MapToAdminOrderResponse(Entities.Order order)
        {
            var voucher = order.UserVoucherStatusId.HasValue
                ? await _context.UserVoucherStatuses
                    .FirstOrDefaultAsync(v => v.UserVoucherStatusId == order.UserVoucherStatusId.Value)
                : null;

            var subTotal = order.OrdersDetails?.Sum(od => od.Price * od.Amount) ?? 0;
            var voucherDiscountAmount = voucher != null ? (decimal)(subTotal * (decimal)voucher.Discount / 100) : 0;
            var userInfo = order.User?.UserInfos.FirstOrDefault();

            // Get status from order
            var orderStatus = order.Status ?? "pending";

            return new AdminOrderResponse
            {
                OrderId = order.OrderId,
                UserId = order.UserId ?? 0,
                Username = order.User?.Username ?? "Unknown",
                CustomerName = userInfo?.FullName,
                CustomerEmail = order.User?.Email,
                PhoneNumber = order.PhoneNumber ?? string.Empty,
                PaymentMethod = order.PaymentMethod,
                DeliveryMethod = order.DeliveryMethod,
                CreatedDate = order.CreatedDate ?? DateTime.Now,
                AddressDescription = order.Address?.Description,
                Status = orderStatus, // Order details status
                StatusPayment = order.StatusPayment ?? "pending", // Payment status
                TotalPrice = order.TotalPrice ?? 0,
                ItemCount = order.OrdersDetails?.Count ?? 0,
                VoucherCode = voucher?.VoucherCode,
                VoucherDiscountAmount = voucherDiscountAmount
            };
        }

        private async Task<AdminOrderDetailResponse> MapToAdminOrderDetailResponse(Entities.Order order)
        {
            var voucher = order.UserVoucherStatusId.HasValue
                ? await _context.UserVoucherStatuses
                    .FirstOrDefaultAsync(v => v.UserVoucherStatusId == order.UserVoucherStatusId.Value)
                : null;

            var subTotal = order.OrdersDetails?.Sum(od => od.Price * od.Amount) ?? 0;
            var voucherDiscountAmount = voucher != null ? (decimal)(subTotal * (decimal)voucher.Discount / 100) : 0;
            var userInfo = order.User?.UserInfos.FirstOrDefault();

            // Get status from order
            var orderStatus = order.Status ?? "pending";

            return new AdminOrderDetailResponse
            {
                OrderId = order.OrderId,
                UserId = order.UserId ?? 0,
                Username = order.User?.Username ?? "Unknown",
                CustomerName = userInfo?.FullName,
                CustomerEmail = order.User?.Email,
                PhoneNumber = order.PhoneNumber ?? string.Empty,
                PaymentMethod = order.PaymentMethod,
                DeliveryMethod = order.DeliveryMethod,
                CreatedDate = order.CreatedDate ?? DateTime.Now,
                UserVoucherStatusId = order.UserVoucherStatusId,
                VoucherCode = voucher?.VoucherCode,
                VoucherDiscount = voucher?.Discount,
                AddressId = order.AddressId ?? 0,
                AddressDescription = order.Address?.Description,
                Status = orderStatus, // Order details status
                StatusPayment = order.StatusPayment ?? "pending", // Payment status
                SubTotal = subTotal,
                ShippingFee = order.ShippingFee ?? 30000m,
                VoucherDiscountAmount = voucherDiscountAmount,
                TotalPrice = order.TotalPrice ?? 0,
                Items = order.OrdersDetails?.Select(od => new AdminOrderItemResponse
                {
                    OrderDetailId = od.OrderDetailId,
                    FlowerId = od.FlowerId ?? 0,
                    FlowerName = od.Flower?.FlowerName ?? string.Empty,
                    FlowerImage = od.Flower?.ImageUrl,
                    UnitPrice = od.Price,
                    Quantity = od.Amount,
                    TotalPrice = od.Price * od.Amount,
                    Status = od.Status ?? "pending",
                    CreatedAt = od.CreatedAt,
                    CategoryName = od.Flower?.Category?.CategoryName
                }).ToList() ?? new List<AdminOrderItemResponse>(),
                Customer = new CustomerInfo
                {
                    UserId = order.UserId ?? 0,
                    Username = order.User?.Username ?? "Unknown",
                    Email = order.User?.Email ?? string.Empty,
                    FullName = userInfo?.FullName,
                    Address = userInfo?.Address,
                    BirthDate = userInfo?.BirthDate,
                    Sex = userInfo?.Sex,
                    CreatedDate = order.User?.CreatedDate,
                    Status = order.User?.Status
                }
            };
        }

        public async Task<int> GetOrderCountAsync(AdminOrderListRequest request)
        {
            try
            {
                var query = _context.Orders.AsQueryable();

                // Apply the same filters as GetAllOrdersAsync
                if (!string.IsNullOrEmpty(request.Status))
                {
                    query = query.Where(o => o.StatusPayment == request.Status);
                }

                if (request.StartDate.HasValue)
                {
                    query = query.Where(o => o.CreatedDate >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    query = query.Where(o => o.CreatedDate <= request.EndDate.Value);
                }

                if (request.UserId.HasValue)
                {
                    query = query.Where(o => o.UserId == request.UserId.Value);
                }

                if (!string.IsNullOrEmpty(request.PaymentMethod))
                {
                    query = query.Where(o => o.PaymentMethod == request.PaymentMethod);
                }

                if (!string.IsNullOrEmpty(request.DeliveryMethod))
                {
                    query = query.Where(o => o.DeliveryMethod == request.DeliveryMethod);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order count: {ex.Message}", ex);
                throw;
            }
        }
    }
}
