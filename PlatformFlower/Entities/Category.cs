using System;
using System.Collections.Generic;

namespace PlatformFlower.Entities;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<FlowerInfo> FlowerInfos { get; set; } = new List<FlowerInfo>();
}
