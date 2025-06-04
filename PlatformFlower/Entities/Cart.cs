using System;
using System.Collections.Generic;

namespace PlatformFlower.Entities;

public partial class Cart
{
    public int CartId { get; set; }

    public int? UserId { get; set; }

    public int? FlowerId { get; set; }

    public int Quantity { get; set; }

    public virtual FlowerInfo? Flower { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual User? User { get; set; }
}
