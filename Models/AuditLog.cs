using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class AuditLog
{
    public int LogId { get; set; }

    public int? UserId { get; set; }

    public string? Action { get; set; }

    public DateTime? LogDate { get; set; }

    public virtual User? User { get; set; }
}
