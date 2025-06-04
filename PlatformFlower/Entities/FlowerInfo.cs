using System;
using System.Collections.Generic;

namespace PlatformFlower.Entities;

public partial class FlowerInfo
{
    public int FlowerId { get; set; }

    public string FlowerName { get; set; } = null!;

    public string? FlowerDescription { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public int AvailableQuantity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? CategoryId { get; set; }

    public int? SellerId { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    public virtual Seller? Seller { get; set; }
}
