using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Claim
{
    public int ClaimId { get; set; }

    public int? PolicyId { get; set; }

    public string? AccidentPlace { get; set; }

    public DateOnly? AccidentDate { get; set; }

    public decimal? InsuredAmount { get; set; }

    public decimal? ClaimAmount { get; set; }

    public string? Status { get; set; }

    public virtual Policy? Policy { get; set; }
}
