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

    public string Status { get; set; } = "active";

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? CategoryId { get; set; }

    public bool IsDeleted { get; set; } = false;

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<OrdersDetail> OrdersDetails { get; set; } = new List<OrdersDetail>();
}
