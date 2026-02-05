using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Estimate
{
    public int EstimateId { get; set; }

    public long EstimateNumber { get; set; }

    public int CustomerId { get; set; }

    public string? CustomerNameSnapshot { get; set; }

    public string? CustomerPhoneSnapshot { get; set; }

    public int VehicleId { get; set; }

    public string? VehicleNameSnapshot { get; set; }

    public string? VehicleModelSnapshot { get; set; }

    public int? InsuranceTypeId { get; set; }

    public string? PolicyTypeSnapshot { get; set; }

    public decimal? VehicleRate { get; set; }

    public decimal? BasePremium { get; set; }

    public decimal? Surcharge { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? EstimatedPremium { get; set; }

    public string? Warranty { get; set; }

    public string? Status { get; set; }

    public DateTime? ValidUntil { get; set; }

    public string? Notes { get; set; }

    public int? CreatedByStaffId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? ApprovedByStaffId { get; set; }

    public DateTime? DecisionAt { get; set; }

    public string? DecisionNote { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual InsuranceType? InsuranceType { get; set; }

    public virtual Vehicle? Vehicle { get; set; }

    public virtual Staff? CreatedByStaff { get; set; }

    public virtual Staff? ApprovedByStaff { get; set; }
}
