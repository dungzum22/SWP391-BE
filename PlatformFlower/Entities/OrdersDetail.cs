using System;
using System.Collections.Generic;

namespace PlatformFlower.Entities;

public partial class OrdersDetail
{
    public int OrderDetailId { get; set; }

    public int? OrderId { get; set; }

    public int? SellerId { get; set; }

    public int? FlowerId { get; set; }

    public decimal Price { get; set; }

    public int Amount { get; set; }

    public int? UserVoucherStatusId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? AddressId { get; set; }

    public string DeliveryMethod { get; set; } = null!;

    public virtual Address? Address { get; set; }

    public virtual FlowerInfo? Flower { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Seller? Seller { get; set; }

    public virtual UserVoucherStatus? UserVoucherStatus { get; set; }
}
