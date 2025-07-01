namespace PlatformFlower.Models.DTOs.Order
{
    public class CreateOrderRequest
    {
        public string PhoneNumber { get; set; } = null!;
        public string PaymentMethod { get; set; } = "VNPay"; // VNPay or COD
        public string DeliveryMethod { get; set; } = "Standard"; // Standard or Express
        public decimal ShippingFee { get; set; } = 30000m; // Any positive value
        public int AddressId { get; set; }
        public int? UserVoucherStatusId { get; set; }
    }
}
