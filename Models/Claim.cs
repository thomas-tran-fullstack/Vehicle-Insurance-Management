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

    public decimal? ClaimableAmount { get; set; }

    public string? Status { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public int? ReviewedByStaffId { get; set; }

    public DateTime? DecisionAt { get; set; }

    public string? DecisionNote { get; set; }

    public virtual Policy? Policy { get; set; }

    public virtual Staff? ReviewedByStaff { get; set; }
}
