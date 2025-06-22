namespace PlatformFlower.Models.DTOs.Order
{
    public class OrderResponse
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string DeliveryMethod { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public int? UserVoucherStatusId { get; set; }
        public string? VoucherCode { get; set; }
        public double? VoucherDiscount { get; set; }
        public int AddressId { get; set; }
        public string? AddressDescription { get; set; }
        public string StatusPayment { get; set; } = "pending";
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal VoucherDiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new();
        public string? PaymentUrl { get; set; }
    }

    public class OrderItemResponse
    {
        public int OrderDetailId { get; set; }
        public int FlowerId { get; set; }
        public string FlowerName { get; set; } = null!;
        public string? FlowerImage { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "pending";
    }
}
