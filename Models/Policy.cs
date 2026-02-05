using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Policy
{
    public int PolicyId { get; set; }

    public long PolicyNumber { get; set; }

    public int CustomerId { get; set; }

    public string? CustomerNameSnapshot { get; set; }

    public string? CustomerAddressSnapshot { get; set; }

    public string? CustomerPhoneSnapshot { get; set; }

    public int VehicleId { get; set; }

    public string? VehicleNumberSnapshot { get; set; }

    public string? VehicleNameSnapshot { get; set; }

    public string? VehicleModelSnapshot { get; set; }

    public string? VehicleVersionSnapshot { get; set; }

    public decimal? VehicleRateSnapshot { get; set; }

    public string? VehicleWarrantySnapshot { get; set; }

    public string? VehicleBodyNumberSnapshot { get; set; }

    public string? VehicleEngineNumberSnapshot { get; set; }

    public int? InsuranceTypeId { get; set; }

    public string? PolicyTypeSnapshot { get; set; }

    public DateOnly? PolicyStartDate { get; set; }

    public DateOnly? PolicyEndDate { get; set; }

    public int? DurationMonths { get; set; }

    public string? Warranty { get; set; }

    public string? AddressProofPath { get; set; }

    public DateOnly? PaymentDueDate { get; set; }

    public decimal? PremiumAmount { get; set; }

    public string? Status { get; set; }

    public int? PendingRenewalMonths { get; set; }

    public DateOnly? PendingRenewalStartDate { get; set; }

    public DateOnly? PendingRenewalEndDate { get; set; }

    public DateOnly? CancelEffectiveDate { get; set; }

    public string? CancellationReason { get; set; }

    // Soft visibility flag used when a customer is deactivated/deleted.
    public bool? IsHidden { get; set; }

    public int? CreatedByStaffId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();

    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();

    public virtual Customer? Customer { get; set; }

    public virtual InsuranceType? InsuranceType { get; set; }

    public virtual ICollection<InsuranceCancellation> InsuranceCancellations { get; set; } = new List<InsuranceCancellation>();

    public virtual ICollection<Penalty> Penalties { get; set; } = new List<Penalty>();

    public virtual Vehicle? Vehicle { get; set; }

    public virtual Staff? CreatedByStaff { get; set; }
}
