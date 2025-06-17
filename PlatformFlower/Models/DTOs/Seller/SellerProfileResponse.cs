namespace PlatformFlower.Models.DTOs.Seller
{
    public class SellerProfileResponse
    {
        public int SellerId { get; set; }
        public int UserId { get; set; }
        public string ShopName { get; set; } = null!;
        public string AddressSeller { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? TotalProduct { get; set; }
        public string Role { get; set; } = null!;
        public string? Introduction { get; set; }
        
        public User.UserResponse? User { get; set; }
    }
}
