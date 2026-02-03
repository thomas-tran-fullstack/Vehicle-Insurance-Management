using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class InsuranceType
{
    public int InsuranceTypeId { get; set; }

    public string TypeCode { get; set; } = null!;

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal BaseRatePercent { get; set; } = 2.50m;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
