using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Validation;
using PlatformFlower.Services.User.Profile;

namespace PlatformFlower.Services.Seller
{
    public class SellerServiceSimple : ISellerService
    {
        private readonly FlowershopContext _context;
        private readonly IProfileService _profileService;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;

        public SellerServiceSimple(
            FlowershopContext context,
            IProfileService profileService,
            IValidationService validationService,
            IAppLogger logger)
        {
            _context = context;
            _profileService = profileService;
            _validationService = validationService;
            _logger = logger;
        }

        public async Task<SellerResponseDto> RegisterSellerAsync(int userId, RegisterSellerDto registerSellerDto)
        {
            try
            {
                _logger.LogInformation($"Starting seller registration for user ID: {userId}");

                // Validate that user exists and is not already a seller
                await ValidateSellerRegistrationAsync(userId, registerSellerDto);

                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    // Update user type to "seller"
                    var user = await _context.Users.FindAsync(userId);
                    if (user == null)
                    {
                        throw new InvalidOperationException("User not found");
                    }

                    user.Type = "seller";
                    _context.Users.Update(user);

                    // Create seller record
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

                    // Return seller response
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
            // Check if user exists
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Check if user is already a seller
            if (await IsUserSellerAsync(userId))
            {
                throw new InvalidOperationException("User is already registered as a seller");
            }

            // Check if shop name is already taken
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
            // Get user information
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
