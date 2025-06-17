namespace PlatformFlower.Models.DTOs.Category
{
    public class CreateCategoryRequest
    {
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Status { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
