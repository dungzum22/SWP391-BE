namespace PlatformFlower.Models.DTOs.Order
{
    public class CreateOrderRequest
    {
        public string PhoneNumber { get; set; } = null!;
        public string PaymentMethod { get; set; } = "VNPay";
        public string DeliveryMethod { get; set; } = "Standard";
        public int AddressId { get; set; }
        public int? UserVoucherStatusId { get; set; }
    }
}
