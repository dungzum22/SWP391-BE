using System;
using System.Collections.Generic;

namespace PlatformFlower.Entities;

public partial class Address
{
    public int AddressId { get; set; }

    public int? UserInfoId { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();

    public virtual UserInfo? UserInfo { get; set; }
}
