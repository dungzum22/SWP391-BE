namespace PlatformFlower.Models.DTOs.Flower
{
    public class CreateFlowerRequest
    {
        public int? FlowerId { get; set; }
        public string? FlowerName { get; set; }
        public string? FlowerDescription { get; set; }
        public decimal? Price { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
        public int? AvailableQuantity { get; set; }
        public string? Status { get; set; }
        public int? CategoryId { get; set; }
        public int? SellerId { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
