using System;
using System.Collections.Generic;

namespace PlatformFlower.Entities;

public partial class Seller
{
    public int SellerId { get; set; }

    public int UserId { get; set; }

    public string ShopName { get; set; } = null!;

    public string AddressSeller { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? TotalProduct { get; set; }

    public string Role { get; set; } = null!;

    public string? Introduction { get; set; }

    public virtual ICollection<FlowerInfo> FlowerInfos { get; set; } = new List<FlowerInfo>();

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UserVoucherStatus> UserVoucherStatuses { get; set; } = new List<UserVoucherStatus>();
}
