namespace PlatformFlower.Models.DTOs.Voucher
{
    public class VoucherResponse
    {
        public int UserVoucherStatusId { get; set; }
        public string VoucherCode { get; set; } = null!;
        public double Discount { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? UsageLimit { get; set; }
        public int? UsageCount { get; set; }
        public int? RemainingCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string Status { get; set; } = "active";
        public bool IsDeleted { get; set; } = false;
        public bool IsExpired => DateTime.Now > EndDate;
        public bool IsActive => DateTime.Now >= StartDate && DateTime.Now <= EndDate && !IsDeleted && Status == "active";
        public bool CanUse => IsActive && (RemainingCount == null || RemainingCount > 0);
        public string DisplayStatus => GetDisplayStatus();

        private string GetDisplayStatus()
        {
            if (IsDeleted)
                return "deleted";
            if (Status == "inactive")
                return "inactive";
            if (DateTime.Now < StartDate)
                return "upcoming";
            if (DateTime.Now > EndDate)
                return "expired";
            if (RemainingCount != null && RemainingCount <= 0)
                return "used_up";
            return "active";
        }
    }

    public class CreateVoucherRequest
    {
        public int? UserVoucherStatusId { get; set; }
        public string? VoucherCode { get; set; }
        public double? Discount { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UsageLimit { get; set; }
        public int? RemainingCount { get; set; }
        public string? Status { get; set; } = "active";
        public bool IsDeleted { get; set; } = false;
    }

    public class VoucherStatsResponse
    {
        public string VoucherCode { get; set; } = null!;
        public int TotalUsers { get; set; }
        public int UsedCount { get; set; }
        public int RemainingCount { get; set; }
        public double UsagePercentage { get; set; }
        public List<VoucherUserStats> UserStats { get; set; } = new List<VoucherUserStats>();
    }

    public class VoucherUserStats
    {
        public int UserInfoId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public int? UsageCount { get; set; }
        public int? RemainingCount { get; set; }
        public string Status { get; set; } = "active";
        public bool HasUsed => (UsageCount ?? 0) > 0;
    }
}
