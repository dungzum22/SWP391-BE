namespace PlatformFlower.Models.DTOs.Flower
{
    public class FlowerResponse
    {
        public int FlowerId { get; set; }
        public string FlowerName { get; set; } = null!;
        public string? FlowerDescription { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int AvailableQuantity { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? SellerId { get; set; }
        public string? SellerShopName { get; set; }
        public bool IsDeleted { get; set; }
    }
}
