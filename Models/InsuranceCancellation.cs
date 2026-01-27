using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class InsuranceCancellation
{
    public int CancellationId { get; set; }

    public int? PolicyId { get; set; }

    public DateOnly? CancelDate { get; set; }

    public decimal? RefundAmount { get; set; }

    public virtual Policy? Policy { get; set; }
}
