using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Estimate
{
    public int EstimateId { get; set; }

    public int? CustomerId { get; set; }

    public int? VehicleId { get; set; }

    public decimal? EstimateAmount { get; set; }

    public string? Warranty { get; set; }

    public string? PolicyType { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Vehicle? Vehicle { get; set; }
}
