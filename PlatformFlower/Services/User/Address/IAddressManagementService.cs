using PlatformFlower.Models.DTOs.Address;

namespace PlatformFlower.Services.User.Address
{
    public interface IAddressManagementService
    {
        Task<AddressResponse> ManageAddressAsync(CreateAddressRequest request, int userId);
        Task<List<AddressResponse>> GetAllAddressesAsync(int userId);
        Task<AddressResponse?> GetAddressByIdAsync(int addressId, int userId);
    }
}
