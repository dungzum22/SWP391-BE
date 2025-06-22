using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Address;

namespace PlatformFlower.Services.User.Address
{
    public static class AddressValidation
    {
        public static async Task ValidateCreateAddressAsync(int userId, CreateAddressRequest request, FlowershopContext context)
        {
            ValidateBasicFields(request, isCreate: true);
            await ValidateUserExists(userId, context);
        }

        public static async Task ValidateUpdateAddressAsync(int userId, CreateAddressRequest request, FlowershopContext context)
        {
            ValidateBasicFields(request, isCreate: false);
            await ValidateUserExists(userId, context);
            await ValidateAddressExists(request.AddressId!.Value, userId, context);
        }

        public static async Task ValidateDeleteAddressAsync(int addressId, int userId, FlowershopContext context)
        {
            await ValidateUserExists(userId, context);
            await ValidateAddressExists(addressId, userId, context);
        }

        public static async Task ValidateGetAddressAsync(int userId, int addressId, FlowershopContext context)
        {
            await ValidateUserExists(userId, context);
            await ValidateAddressExists(addressId, userId, context);
        }

        private static void ValidateBasicFields(CreateAddressRequest request, bool isCreate)
        {
            if (!isCreate && (request.AddressId == null || request.AddressId <= 0))
            {
                throw new ArgumentException("Address ID is required for update operations");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                throw new ArgumentException("Address description is required");
            }

            if (request.Description.Length < 5)
            {
                throw new ArgumentException("Address description must be at least 5 characters long");
            }

            if (request.Description.Length > 255)
            {
                throw new ArgumentException("Address description must not exceed 255 characters");
            }

            if (!IsValidAddressFormat(request.Description))
            {
                throw new ArgumentException("Address description contains invalid characters");
            }
        }

        private static async Task ValidateUserExists(int userId, FlowershopContext context)
        {
            var userExists = await context.Users.AnyAsync(u => u.UserId == userId);
            if (!userExists)
            {
                throw new ArgumentException($"User with ID {userId} not found");
            }

            var userInfo = await context.UserInfos.FirstOrDefaultAsync(ui => ui.UserId == userId);
            if (userInfo == null)
            {
                throw new InvalidOperationException("User profile not found. Please complete your profile first.");
            }
        }

        private static async Task ValidateAddressExists(int addressId, int userId, FlowershopContext context)
        {
            var userInfo = await context.UserInfos.FirstOrDefaultAsync(ui => ui.UserId == userId);
            if (userInfo == null)
            {
                throw new InvalidOperationException("User profile not found");
            }

            var addressExists = await context.Addresses
                .AnyAsync(a => a.AddressId == addressId && a.UserInfoId == userInfo.UserInfoId);
            
            if (!addressExists)
            {
                throw new ArgumentException($"Address with ID {addressId} not found or does not belong to user");
            }
        }

        private static bool IsValidAddressFormat(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            var invalidChars = new char[] { '<', '>', '"', '\'', '&', '\0', '\r', '\n', '\t' };
            return !address.Any(c => invalidChars.Contains(c));
        }
    }
}
