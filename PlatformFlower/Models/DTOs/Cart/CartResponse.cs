namespace PlatformFlower.Models.DTOs.Cart
{
    public class CartResponse
    {
        public List<CartItemResponse> Items { get; set; } = new();
        public CartSummary Summary { get; set; } = new();
    }

    public class CartSummary
    {
        public decimal GrandTotal { get; set; }
        public int TotalItems { get; set; }
        public int TotalTypes { get; set; }
    }
}
