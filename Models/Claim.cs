using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Claim
{
    public int ClaimId { get; set; }

    public long? ClaimNumber { get; set; }

    public int? PolicyId { get; set; }

    public string? AccidentPlace { get; set; }

    public DateOnly? AccidentDate { get; set; }

    public decimal? InsuredAmount { get; set; }

    public decimal? ClaimableAmount { get; set; } // Will be populated by staff during inspection, initially null

    public string? Status { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? ReviewedByStaffId { get; set; }

    public DateTime? DecisionAt { get; set; }

    public string? DecisionNote { get; set; }

    // New properties for claim management workflow
    public string? CustomerNameSnapshot { get; set; }
    public DateOnly? PolicyStartDateSnapshot { get; set; }
    public DateOnly? PolicyEndDateSnapshot { get; set; }
    public string? Description { get; set; }
    public string? DocumentPath { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public int? ApprovedByStaffId { get; set; }
    public DateTime? PaidAt { get; set; }

    public virtual Policy? Policy { get; set; }

    public virtual Staff? ReviewedByStaff { get; set; }

    public virtual Staff? ApprovedByStaff { get; set; }
}
