using PlatformFlower.Models.DTOs.Cart;

namespace PlatformFlower.Services.User.Cart
{
    public static class CartValidation
    {
        public static void ValidateAddToCartRequest(AddToCartRequest request)
        {
            if (request.FlowerId <= 0)
                throw new ArgumentException("Invalid flower ID");

            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0");

            if (request.Quantity > 100)
                throw new ArgumentException("Cannot add more than 100 items at once");
        }

        public static void ValidateUpdateCartRequest(UpdateCartRequest request)
        {
            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0");

            if (request.Quantity > 100)
                throw new ArgumentException("Quantity cannot exceed 100 items");
        }

        public static void ValidateUserId(int userId)
        {
            if (userId <= 0)
                throw new ArgumentException("Invalid user ID");
        }

        public static void ValidateCartId(int cartId)
        {
            if (cartId <= 0)
                throw new ArgumentException("Invalid cart ID");
        }
    }
}
