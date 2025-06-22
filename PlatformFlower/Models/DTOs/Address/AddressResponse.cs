namespace PlatformFlower.Models.DTOs.Address
{
    public class AddressResponse
    {
        public int AddressId { get; set; }
        public int UserInfoId { get; set; }
        public string Description { get; set; } = null!;
        public string? UserFullName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
