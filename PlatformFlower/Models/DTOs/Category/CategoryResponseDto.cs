namespace PlatformFlower.Models.DTOs.Category
{
    public class CategoryResponseDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int FlowerCount { get; set; } // Number of flowers in this category
    }
}
