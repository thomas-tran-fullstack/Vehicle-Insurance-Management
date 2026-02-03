using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int? ToUserId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Channel { get; set; } = "IN_APP";

    public string Status { get; set; } = "QUEUED";

    public DateTime CreatedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public virtual User? User { get; set; }
}
