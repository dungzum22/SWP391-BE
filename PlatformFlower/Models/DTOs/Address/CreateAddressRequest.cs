namespace PlatformFlower.Models.DTOs.Address
{
    public class CreateAddressRequest
    {
        public int? AddressId { get; set; }
        public string? Description { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
