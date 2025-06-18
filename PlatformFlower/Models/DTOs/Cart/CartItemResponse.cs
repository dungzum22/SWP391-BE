namespace PlatformFlower.Models.DTOs.Cart
{
    public class CartItemResponse
    {
        public int CartId { get; set; }
        public int FlowerId { get; set; }
        public string FlowerName { get; set; } = string.Empty;
        public string? FlowerDescription { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public decimal CurrentPrice { get; set; }
        public bool PriceChanged => UnitPrice != CurrentPrice;
        public string? CategoryName { get; set; }
        public string? SellerShopName { get; set; }
        public int AvailableQuantity { get; set; }
    }
}
