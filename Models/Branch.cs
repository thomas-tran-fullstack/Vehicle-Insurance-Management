using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Branch
{
    public int BranchId { get; set; }

    public string BranchName { get; set; } = null!;

    public string ManagerName { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string Hotline { get; set; } = null!;

    public string Email { get; set; } = null!;

    public TimeOnly OperatingStartTime { get; set; }

    public TimeOnly OperatingEndTime { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }
}
