using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Policy
{
    public int PolicyId { get; set; }

    public int? CustomerId { get; set; }

    public int? VehicleId { get; set; }

    public DateOnly? PolicyDate { get; set; }

    public int? Duration { get; set; }

    public string? Warranty { get; set; }

    public string? Status { get; set; }

    // Soft visibility flag used when a customer is deactivated/deleted.
    public bool? IsHidden { get; set; }

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<InsuranceCancellation> InsuranceCancellations { get; set; } = new List<InsuranceCancellation>();

    public virtual ICollection<Penalty> Penalties { get; set; } = new List<Penalty>();

    public virtual Vehicle? Vehicle { get; set; }
}
