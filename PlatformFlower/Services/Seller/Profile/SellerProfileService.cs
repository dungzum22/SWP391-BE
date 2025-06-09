using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.User.Profile;

namespace PlatformFlower.Services.Seller.Profile
{
    public class SellerProfileService : ISellerProfileService
    {
        private readonly FlowershopContext _context;
        private readonly IProfileService _profileService;
        private readonly IAppLogger _logger;

        public SellerProfileService(
            FlowershopContext context,
            IProfileService profileService,
            IAppLogger logger)
        {
            _context = context;
            _profileService = profileService;
            _logger = logger;
        }

        public async Task<SellerResponseDto> UpsertSellerAsync(int userId, UpdateSellerDto sellerDto)
        {
            try
            {
                _logger.LogInformation($"Starting seller upsert for user ID: {userId}");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var existingSeller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);

                if (existingSeller == null)
                {
                    return await CreateNewSellerAsync(user, sellerDto);
                }
                else
                {
                    return await UpdateExistingSellerAsync(existingSeller, sellerDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during seller upsert for user ID {userId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<SellerResponseDto?> GetSellerByUserIdAsync(int userId)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .ThenInclude(u => u.UserInfos)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            return seller != null ? await MapToSellerResponseDto(seller) : null;
        }

        public async Task<SellerResponseDto?> GetSellerByIdAsync(int sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .ThenInclude(u => u.UserInfos)
                .FirstOrDefaultAsync(s => s.SellerId == sellerId);

            return seller != null ? await MapToSellerResponseDto(seller) : null;
        }

        public async Task<bool> IsUserSellerAsync(int userId)
        {
            return await _context.Sellers.AnyAsync(s => s.UserId == userId);
        }

        private async Task<SellerResponseDto> CreateNewSellerAsync(Entities.User user, UpdateSellerDto sellerDto)
        {
            _logger.LogInformation($"Creating new seller for user ID: {user.UserId}");

            await ValidateShopNameAsync(null, sellerDto.ShopName);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                user.Type = "seller";
                _context.Users.Update(user);

                var seller = new Entities.Seller
                {
                    UserId = user.UserId,
                    ShopName = sellerDto.ShopName,
                    AddressSeller = sellerDto.AddressSeller,
                    Role = sellerDto.Role,
                    Introduction = sellerDto.Introduction,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    TotalProduct = 0
                };

                _context.Sellers.Add(seller);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"New seller created successfully for user ID: {user.UserId}, seller ID: {seller.SellerId}");

                return await MapToSellerResponseDto(seller);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<SellerResponseDto> UpdateExistingSellerAsync(Entities.Seller seller, UpdateSellerDto sellerDto)
        {
            _logger.LogInformation($"Updating existing seller for user ID: {seller.UserId}");

            await ValidateShopNameAsync(seller.SellerId, sellerDto.ShopName);

            seller.ShopName = sellerDto.ShopName;
            seller.AddressSeller = sellerDto.AddressSeller;
            seller.Role = sellerDto.Role;
            seller.Introduction = sellerDto.Introduction;
            seller.UpdatedAt = DateTime.UtcNow;

            _context.Sellers.Update(seller);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Seller updated successfully for user ID: {seller.UserId}, seller ID: {seller.SellerId}");

            return await MapToSellerResponseDto(seller);
        }

        private async Task ValidateShopNameAsync(int? sellerId, string shopName)
        {
            var query = _context.Sellers.Where(s => s.ShopName.ToLower() == shopName.ToLower());

            if (sellerId.HasValue)
            {
                query = query.Where(s => s.SellerId != sellerId.Value);
            }

            var existingShop = await query.FirstOrDefaultAsync();

            if (existingShop != null)
            {
                throw new InvalidOperationException("Shop name is already taken");
            }

            _logger.LogInformation($"Shop name validation passed for: {shopName}");
        }

        private async Task<SellerResponseDto> MapToSellerResponseDto(Entities.Seller seller)
        {
            var userResponse = await _profileService.GetUserByIdAsync(seller.UserId);

            return new SellerResponseDto
            {
                SellerId = seller.SellerId,
                UserId = seller.UserId,
                ShopName = seller.ShopName,
                AddressSeller = seller.AddressSeller,
                CreatedAt = seller.CreatedAt,
                UpdatedAt = seller.UpdatedAt,
                TotalProduct = seller.TotalProduct,
                Role = seller.Role,
                Introduction = seller.Introduction,
                User = userResponse
            };
        }
    }
}
