namespace PlatformFlower.Models.DTOs.Order
{
    public class AdminOrderListRequest
    {
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UserId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? DeliveryMethod { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedDate";
        public string? SortOrder { get; set; } = "desc";
    }

    public class AdminOrderResponse
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string DeliveryMethod { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public string? AddressDescription { get; set; }
        public string Status { get; set; } = "pending"; // Order details status
        public string StatusPayment { get; set; } = "pending"; // Payment status
        public decimal TotalPrice { get; set; }
        public int ItemCount { get; set; }
        public string? VoucherCode { get; set; }
        public decimal VoucherDiscountAmount { get; set; }
    }

    public class AdminOrderDetailResponse
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string DeliveryMethod { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public int? UserVoucherStatusId { get; set; }
        public string? VoucherCode { get; set; }
        public double? VoucherDiscount { get; set; }
        public int AddressId { get; set; }
        public string? AddressDescription { get; set; }
        public string Status { get; set; } = "pending"; // Order details status
        public string StatusPayment { get; set; } = "pending"; // Payment status
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal VoucherDiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public List<AdminOrderItemResponse> Items { get; set; } = new();
        public CustomerInfo Customer { get; set; } = new();
    }

    public class AdminOrderItemResponse
    {
        public int OrderDetailId { get; set; }
        public int FlowerId { get; set; }
        public string FlowerName { get; set; } = null!;
        public string? FlowerImage { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime? CreatedAt { get; set; }
        public string? CategoryName { get; set; }
    }

    public class CustomerInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? Sex { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? Status { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = null!;
    }

    public class OrderStatisticsResponse
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int AcceptedOrders { get; set; }
        public int PendingDeliveryOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CanceledOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<DailyOrderStats> DailyStats { get; set; } = new();
        public List<PaymentMethodStats> PaymentMethodStats { get; set; } = new();
        public List<TopCustomerStats> TopCustomers { get; set; } = new();
    }

    public class DailyOrderStats
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class PaymentMethodStats
    {
        public string PaymentMethod { get; set; } = null!;
        public int OrderCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class TopCustomerStats
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string? CustomerName { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class PaginatedOrderResponse
    {
        public List<AdminOrderResponse> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
