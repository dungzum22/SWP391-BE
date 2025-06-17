namespace PlatformFlower.Models.DTOs.Seller
{
    public class UpdateSellerRequest
    {
        public string ShopName { get; set; } = null!;
        public string AddressSeller { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string? Introduction { get; set; }
    }
}
