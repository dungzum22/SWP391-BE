using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Seller;
using System.Text.RegularExpressions;

namespace PlatformFlower.Services.Seller.Profile
{
    public static class SellerProfileValidation
    {
        public static async Task ValidateUpsertSellerAsync(int userId, UpdateSellerRequest request, FlowershopContext context)
        {
            ValidateSellerFields(request);
            await ValidateUserEligibility(userId, context);
            await ValidateShopNameAsync(null, request.ShopName, context);
        }

        public static async Task ValidateUpdateSellerAsync(int sellerId, int userId, UpdateSellerRequest request, FlowershopContext context)
        {
            ValidateSellerFields(request);
            await ValidateUserEligibility(userId, context);
            await ValidateShopNameAsync(sellerId, request.ShopName, context);
        }

        private static void ValidateSellerFields(UpdateSellerRequest request)
        {
            ValidateShopName(request.ShopName);
            ValidateSellerAddress(request.AddressSeller);
            ValidateSellerRole(request.Role);
            ValidateIntroduction(request.Introduction);
        }

        private static void ValidateShopName(string shopName)
        {
            if (string.IsNullOrWhiteSpace(shopName))
            {
                throw new ArgumentException("Shop name is required");
            }

            if (shopName.Length < 2)
            {
                throw new ArgumentException("Shop name must be at least 2 characters long");
            }

            if (shopName.Length > 255)
            {
                throw new ArgumentException("Shop name must not exceed 255 characters");
            }

            if (!IsValidShopNameFormat(shopName))
            {
                throw new ArgumentException("Shop name contains invalid characters. Only letters, numbers, spaces, and basic punctuation are allowed");
            }

            if (ContainsInappropriateContent(shopName))
            {
                throw new ArgumentException("Shop name contains inappropriate content");
            }
        }

        private static void ValidateSellerAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Seller address is required");
            }

            if (address.Length < 5)
            {
                throw new ArgumentException("Seller address must be at least 5 characters long");
            }

            if (address.Length > 255)
            {
                throw new ArgumentException("Seller address must not exceed 255 characters");
            }

            if (!IsValidAddressFormat(address))
            {
                throw new ArgumentException("Seller address contains invalid characters");
            }
        }

        private static void ValidateSellerRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentException("Role is required");
            }

            var validRoles = new[] { "individual", "enterprise" };
            if (!validRoles.Contains(role.ToLower()))
            {
                throw new ArgumentException("Role must be either 'individual' or 'enterprise'");
            }
        }

        private static void ValidateIntroduction(string? introduction)
        {
            if (introduction != null)
            {
                if (introduction.Length > 1000)
                {
                    throw new ArgumentException("Introduction must not exceed 1000 characters");
                }

                if (ContainsInappropriateContent(introduction))
                {
                    throw new ArgumentException("Introduction contains inappropriate content");
                }
            }
        }

        private static async Task ValidateUserEligibility(int userId, FlowershopContext context)
        {
            var user = await context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            if (user.Status != "active")
            {
                throw new InvalidOperationException("Only active users can become sellers");
            }
        }

        public static async Task ValidateShopNameAsync(int? sellerId, string shopName, FlowershopContext context)
        {
            var query = context.Sellers.Where(s => s.ShopName.ToLower() == shopName.ToLower());

            if (sellerId.HasValue)
            {
                query = query.Where(s => s.SellerId != sellerId.Value);
            }

            var existingShop = await query.FirstOrDefaultAsync();

            if (existingShop != null)
            {
                throw new InvalidOperationException("Shop name is already taken");
            }
        }

        private static bool IsValidShopNameFormat(string shopName)
        {
            var shopNameRegex = new Regex(@"^[a-zA-Z0-9\s\-_.,&'()]+$");
            return shopNameRegex.IsMatch(shopName);
        }

        private static bool IsValidAddressFormat(string address)
        {
            var addressRegex = new Regex(@"^[a-zA-Z0-9\s\-_.,#/()]+$");
            return addressRegex.IsMatch(address);
        }

        private static bool ContainsInappropriateContent(string content)
        {
            var inappropriateWords = new[]
            {
                "spam", "scam", "fake", "fraud", "illegal", "banned", "prohibited"
            };

            return inappropriateWords.Any(word => 
                content.ToLower().Contains(word.ToLower()));
        }

        public static void ValidateGetSellerRequest(int userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentException("User ID must be a positive integer");
            }
        }

        public static void ValidateGetSellerByIdRequest(int sellerId)
        {
            if (sellerId <= 0)
            {
                throw new ArgumentException("Seller ID must be a positive integer");
            }
        }
    }
}
