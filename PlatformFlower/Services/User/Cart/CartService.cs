using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Cart;

namespace PlatformFlower.Services.User.Cart
{
    public class CartService : ICartService
    {
        private readonly FlowershopContext _context;

        public CartService(FlowershopContext context)
        {
            _context = context;
        }

        public async Task<CartItemResponse> AddToCartAsync(int userId, AddToCartRequest request)
        {
            var flower = await _context.FlowerInfos
                .Include(f => f.Category)
                .FirstOrDefaultAsync(f => f.FlowerId == request.FlowerId && !f.IsDeleted && f.Status == "active");

            if (flower == null)
                throw new InvalidOperationException("Flower not found or not available");

            if (flower.AvailableQuantity < request.Quantity)
                throw new InvalidOperationException($"Only {flower.AvailableQuantity} items available in stock");

            var existingCartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.FlowerId == request.FlowerId);

            if (existingCartItem != null)
            {
                var newQuantity = existingCartItem.Quantity + request.Quantity;
                if (flower.AvailableQuantity < newQuantity)
                    throw new InvalidOperationException($"Cannot add {request.Quantity} more items. Only {flower.AvailableQuantity - existingCartItem.Quantity} more available");

                existingCartItem.Quantity = newQuantity;
                await _context.SaveChangesAsync();

                return await MapToCartItemResponse(existingCartItem, flower);
            }
            else
            {
                var cartItem = new Entities.Cart
                {
                    UserId = userId,
                    FlowerId = request.FlowerId,
                    Quantity = request.Quantity,
                    UnitPrice = flower.Price
                };

                _context.Carts.Add(cartItem);
                await _context.SaveChangesAsync();

                return await MapToCartItemResponse(cartItem, flower);
            }
        }

        public async Task<CartResponse> GetCartAsync(int userId)
        {
            var cartItems = await _context.Carts
                .Include(c => c.Flower)
                    .ThenInclude(f => f.Category)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var response = new CartResponse();

            foreach (var cartItem in cartItems)
            {
                if (cartItem.Flower != null && !cartItem.Flower.IsDeleted)
                {
                    var itemResponse = await MapToCartItemResponse(cartItem, cartItem.Flower);
                    response.Items.Add(itemResponse);
                }
            }

            response.Summary = CalculateCartSummary(response.Items);
            return response;
        }

        public async Task<CartItemResponse> UpdateCartItemAsync(int userId, int cartId, UpdateCartRequest request)
        {
            var cartItem = await _context.Carts
                .Include(c => c.Flower)
                    .ThenInclude(f => f.Category)
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userId);

            if (cartItem == null)
                throw new InvalidOperationException("Cart item not found");

            if (cartItem.Flower == null || cartItem.Flower.IsDeleted || cartItem.Flower.Status != "active")
                throw new InvalidOperationException("Flower is no longer available");

            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0");

            if (cartItem.Flower.AvailableQuantity < request.Quantity)
                throw new InvalidOperationException($"Only {cartItem.Flower.AvailableQuantity} items available in stock");

            cartItem.Quantity = request.Quantity;
            await _context.SaveChangesAsync();

            return await MapToCartItemResponse(cartItem, cartItem.Flower);
        }

        public async Task RemoveCartItemAsync(int userId, int cartId)
        {
            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userId);

            if (cartItem == null)
                throw new InvalidOperationException("Cart item not found");

            _context.Carts.Remove(cartItem);
            await _context.SaveChangesAsync();
        }

        public async Task ClearCartAsync(int userId)
        {
            await _context.Carts
                .Where(c => c.UserId == userId)
                .ExecuteDeleteAsync();
        }

        public async Task<int> GetCartItemCountAsync(int userId)
        {
            return await _context.Carts
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);
        }

        private async Task<CartItemResponse> MapToCartItemResponse(Entities.Cart cartItem, Entities.FlowerInfo flower)
        {
            return new CartItemResponse
            {
                CartId = cartItem.CartId,
                FlowerId = flower.FlowerId,
                FlowerName = flower.FlowerName,
                FlowerDescription = flower.FlowerDescription,
                ImageUrl = flower.ImageUrl,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice,
                CurrentPrice = flower.Price,
                CategoryName = flower.Category?.CategoryName,
                AvailableQuantity = flower.AvailableQuantity
            };
        }

        private CartSummary CalculateCartSummary(List<CartItemResponse> items)
        {
            var grandTotal = items.Sum(i => i.TotalPrice);
            var totalItems = items.Sum(i => i.Quantity);

            return new CartSummary
            {
                GrandTotal = grandTotal,
                TotalItems = totalItems,
                TotalTypes = items.Count
            };
        }
    }
}
