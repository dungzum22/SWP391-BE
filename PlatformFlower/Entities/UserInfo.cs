using System;
using System.Collections.Generic;

namespace PlatformFlower.Entities;

public partial class UserInfo
{
    public int UserInfoId { get; set; }

    public int? UserId { get; set; }

    public string? Address { get; set; }

    public string? FullName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Sex { get; set; }

    public bool? IsSeller { get; set; }

    public string? Avatar { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public int? Points { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual User? User { get; set; }

    public virtual ICollection<UserVoucherStatus> UserVoucherStatuses { get; set; } = new List<UserVoucherStatus>();
}
