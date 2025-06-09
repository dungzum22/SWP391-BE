using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.User.Profile;

namespace PlatformFlower.Services.Seller
{
    public class SellerServiceSimple : ISellerService
    {
        private readonly FlowershopContext _context;
        private readonly IProfileService _profileService;
        private readonly IAppLogger _logger;

        public SellerServiceSimple(
            FlowershopContext context,
            IProfileService profileService,
            IAppLogger logger)
        {
            _context = context;
            _profileService = profileService;
            _logger = logger;
        }

        public async Task<SellerResponseDto> RegisterSellerAsync(int userId, RegisterSellerDto registerSellerDto)
        {
            try
            {
                _logger.LogInformation($"Starting seller registration for user ID: {userId}");

                await ValidateSellerRegistrationAsync(userId, registerSellerDto);

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user == null)
                    {
                        throw new InvalidOperationException("User not found");
                    }

                    user.Type = "seller";
                    _context.Users.Update(user);

                    var seller = new Entities.Seller
                    {
                        UserId = userId,
                        ShopName = registerSellerDto.ShopName,
                        AddressSeller = registerSellerDto.AddressSeller,
                        Role = registerSellerDto.Role,
                        Introduction = registerSellerDto.Introduction,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        TotalProduct = 0
                    };

                    _context.Sellers.Add(seller);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Seller registered successfully for user ID: {userId}, seller ID: {seller.SellerId}");

                    return await MapToSellerResponseDto(seller);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during seller registration for user ID {userId}: {ex.Message}", ex);
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

        private async Task ValidateSellerRegistrationAsync(int userId, RegisterSellerDto registerSellerDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            if (await IsUserSellerAsync(userId))
            {
                throw new InvalidOperationException("User is already registered as a seller");
            }

            var existingShop = await _context.Sellers
                .FirstOrDefaultAsync(s => s.ShopName.ToLower() == registerSellerDto.ShopName.ToLower());

            if (existingShop != null)
            {
                throw new InvalidOperationException("Shop name is already taken");
            }

            _logger.LogInformation($"Seller registration validation passed for user ID: {userId}");
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
