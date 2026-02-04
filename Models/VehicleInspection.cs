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

    public virtual Staff? Staff { get; set; }

    public virtual Vehicle? Vehicle { get; set; }
}
