using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Penalty
{
    public int PenaltyId { get; set; }

    public int? PolicyId { get; set; }

    public string? Reason { get; set; }

    public decimal? Amount { get; set; }

    public string? Status { get; set; }

    public virtual Policy? Policy { get; set; }
}
