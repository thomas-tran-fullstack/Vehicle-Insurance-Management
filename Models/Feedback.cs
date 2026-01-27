using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int? CustomerId { get; set; }

    public string? Content { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Customer? Customer { get; set; }
}
