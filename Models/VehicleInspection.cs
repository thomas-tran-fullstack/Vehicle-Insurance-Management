using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class VehicleInspection
{
    public int InspectionId { get; set; }

    public int? VehicleId { get; set; }

    public int? AssignedStaffId { get; set; }

    public DateTime? ScheduledDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public string? Status { get; set; }

    public string? Result { get; set; }

    // New properties for vehicle inspection dispatch workflow
    public int? ClaimId { get; set; }
    public string? InspectionLocation { get; set; }
    public string? OverallAssessment { get; set; }
    public bool? ConfirmedCorrect { get; set; }
    public string? DocumentPath { get; set; }
    public int? VerifiedByStaffId { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public virtual Staff? Staff { get; set; }

    public virtual Vehicle? Vehicle { get; set; }

    public virtual Claim? Claim { get; set; }

    public virtual Staff? VerifiedByStaff { get; set; }
}
