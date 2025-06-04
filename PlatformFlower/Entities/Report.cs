using System;
using System.Collections.Generic;

namespace PlatformFlower.Entities;

public partial class Report
{
    public int ReportId { get; set; }

    public int UserId { get; set; }

    public int FlowerId { get; set; }

    public int SellerId { get; set; }

    public string ReportReason { get; set; } = null!;

    public string? ReportDescription { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual FlowerInfo Flower { get; set; } = null!;

    public virtual Seller Seller { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
