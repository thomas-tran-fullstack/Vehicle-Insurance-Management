using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Staff
{
    public int StaffId { get; set; }

    public int UserId { get; set; }

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public string? Position { get; set; }

    public bool? IsActive { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<VehicleInspection> VehicleInspections { get; set; } = new List<VehicleInspection>();
}
