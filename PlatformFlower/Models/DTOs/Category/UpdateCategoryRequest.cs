namespace PlatformFlower.Models.DTOs.Category
{
    public class UpdateCategoryRequest
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? Status { get; set; }
    }
}
