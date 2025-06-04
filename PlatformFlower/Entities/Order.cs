using System;
using System.Collections.Generic;

namespace PlatformFlower.Entities;

public partial class Order
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public string? PhoneNumber { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string DeliveryMethod { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public int? UserVoucherStatusId { get; set; }

    public int? AddressId { get; set; }

    public int? CartId { get; set; }

    public string? StatusPayment { get; set; }

    public decimal? TotalPrice { get; set; }

    public virtual Address? Address { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();

    public virtual User? User { get; set; }

    public virtual UserVoucherStatus? UserVoucherStatus { get; set; }
}
