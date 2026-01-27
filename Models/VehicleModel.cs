using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class VehicleModel
{
    public int ModelId { get; set; }

    public string? ModelName { get; set; }

    public string? VehicleClass { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
