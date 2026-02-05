using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int? UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? Type { get; set; }

    public string Channel { get; set; } = "IN_APP";

    public string Status { get; set; } = "QUEUED";

    public bool IsRead { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? SentAt { get; set; }

    public virtual User? User { get; set; }
}

