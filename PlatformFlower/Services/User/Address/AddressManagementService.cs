using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Address;
using PlatformFlower.Services.Common.Logging;

namespace PlatformFlower.Services.User.Address
{
    public class AddressManagementService : IAddressManagementService
    {
        private readonly FlowershopContext _context;
        private readonly IAppLogger _logger;

        public AddressManagementService(FlowershopContext context, IAppLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AddressResponse> ManageAddressAsync(CreateAddressRequest request, int userId)
        {
            if (request.AddressId == null || request.AddressId == 0)
            {
                await AddressValidation.ValidateCreateAddressAsync(userId, request, _context);
                return await CreateAddressAsync(request, userId);
            }
            else if (request.IsDeleted)
            {
                await AddressValidation.ValidateDeleteAddressAsync(request.AddressId.Value, userId, _context);
                return await DeleteAddressAsync(request.AddressId.Value, userId);
            }
            else
            {
                await AddressValidation.ValidateUpdateAddressAsync(userId, request, _context);
                return await UpdateAddressAsync(request, userId);
            }
        }

        public async Task<List<AddressResponse>> GetAllAddressesAsync(int userId)
        {
            try
            {
                _logger.LogInformation($"Getting all addresses for user: {userId}");

                var userInfo = await _context.UserInfos
                    .FirstOrDefaultAsync(ui => ui.UserId == userId);

                if (userInfo == null)
                {
                    throw new InvalidOperationException("User profile not found");
                }

                var addresses = await _context.Addresses
                    .Where(a => a.UserInfoId == userInfo.UserInfoId)
                    .ToListAsync();

                var addressResponses = addresses.Select(a => MapToAddressResponse(a, userInfo.FullName)).ToList();

                _logger.LogInformation($"Successfully retrieved {addressResponses.Count} addresses for user: {userId}");
                return addressResponses;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting addresses for user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<AddressResponse?> GetAddressByIdAsync(int addressId, int userId)
        {
            try
            {
                _logger.LogInformation($"Getting address {addressId} for user: {userId}");

                await AddressValidation.ValidateGetAddressAsync(userId, addressId, _context);

                var userInfo = await _context.UserInfos
                    .FirstOrDefaultAsync(ui => ui.UserId == userId);

                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserInfoId == userInfo!.UserInfoId);

                if (address == null)
                {
                    _logger.LogWarning($"Address {addressId} not found for user: {userId}");
                    return null;
                }

                _logger.LogInformation($"Successfully retrieved address for user: {userId}");
                return MapToAddressResponse(address, userInfo!.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting address for user: {ex.Message}", ex);
                throw;
            }
        }

        private async Task<AddressResponse> CreateAddressAsync(CreateAddressRequest request, int userId)
        {
            try
            {
                _logger.LogInformation($"Creating address for user: {userId}");

                var userInfo = await _context.UserInfos
                    .FirstOrDefaultAsync(ui => ui.UserId == userId);

                var address = new Entities.Address
                {
                    UserInfoId = userInfo!.UserInfoId,
                    Description = request.Description!.Trim()
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Address created successfully for user: {userId}, AddressId: {address.AddressId}");
                return MapToAddressResponse(address, userInfo.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating address for user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        private async Task<AddressResponse> UpdateAddressAsync(CreateAddressRequest request, int userId)
        {
            try
            {
                _logger.LogInformation($"Updating address {request.AddressId} for user: {userId}");

                var userInfo = await _context.UserInfos
                    .FirstOrDefaultAsync(ui => ui.UserId == userId);

                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.AddressId == request.AddressId && a.UserInfoId == userInfo!.UserInfoId);

                if (address == null)
                {
                    throw new InvalidOperationException($"Address with ID {request.AddressId} not found");
                }

                address.Description = request.Description!.Trim();

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Address updated successfully for user: {userId}, AddressId: {address.AddressId}");
                return MapToAddressResponse(address, userInfo!.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating address for user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        private async Task<AddressResponse> DeleteAddressAsync(int addressId, int userId)
        {
            try
            {
                _logger.LogInformation($"Deleting address {addressId} for user: {userId}");

                var userInfo = await _context.UserInfos
                    .FirstOrDefaultAsync(ui => ui.UserId == userId);

                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserInfoId == userInfo!.UserInfoId);

                if (address == null)
                {
                    throw new InvalidOperationException($"Address with ID {addressId} not found");
                }

                var addressResponse = MapToAddressResponse(address, userInfo!.FullName);

                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Address deleted successfully for user: {userId}, AddressId: {addressId}");
                return addressResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting address {addressId} for user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        private AddressResponse MapToAddressResponse(Entities.Address address, string? userFullName)
        {
            return new AddressResponse
            {
                AddressId = address.AddressId,
                UserInfoId = address.UserInfoId ?? 0,
                Description = address.Description ?? string.Empty,
                UserFullName = userFullName,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
    }
}
