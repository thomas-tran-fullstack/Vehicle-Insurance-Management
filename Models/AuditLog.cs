using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class AuditLog
{
    public long LogId { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string? Entity { get; set; }

    public string? EntityId { get; set; }

    public string? Meta { get; set; }

    public DateTime LogDate { get; set; }

    public virtual User? User { get; set; }
}
