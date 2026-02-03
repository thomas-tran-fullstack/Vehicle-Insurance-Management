using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Policy
{
    public int PolicyId { get; set; }

    public long PolicyNumber { get; set; }

    public int CustomerId { get; set; }

    public int VehicleId { get; set; }

    public int InsuranceTypeId { get; set; }

    public DateOnly PolicyStartDate { get; set; }

    public DateOnly PolicyEndDate { get; set; }

    public int DurationMonths { get; set; }

    public string? Warranty { get; set; }

    public string? AddressProofPath { get; set; }

    public decimal PremiumAmount { get; set; }

    public string Status { get; set; } = "ACTIVE";

    public bool IsHidden { get; set; }

    public int? CreatedByStaffId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();

    public virtual Customer Customer { get; set; }

    public virtual ICollection<InsuranceCancellation> InsuranceCancellations { get; set; } = new List<InsuranceCancellation>();

    public virtual ICollection<Penalty> Penalties { get; set; } = new List<Penalty>();

    public virtual Vehicle Vehicle { get; set; }

    public virtual InsuranceType InsuranceType { get; set; }

    public virtual Staff? CreatedByStaff { get; set; }
}
