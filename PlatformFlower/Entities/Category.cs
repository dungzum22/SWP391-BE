using System;
using System.Collections.Generic;

namespace PlatformFlower.Entities;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<FlowerInfo> FlowerInfos { get; set; } = new List<FlowerInfo>();
}
