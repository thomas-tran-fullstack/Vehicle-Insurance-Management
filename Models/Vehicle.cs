using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Vehicle
{
    public int VehicleId { get; set; }

    public int? CustomerId { get; set; }

    public int? ModelId { get; set; }

    public string? VehicleName { get; set; }

    public string? VehicleVersion { get; set; }

    public decimal? VehicleRate { get; set; }

    public string? BodyNumber { get; set; }

    public string? EngineNumber { get; set; }

    public string? VehicleNumber { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Estimate> Estimates { get; set; } = new List<Estimate>();

    public virtual VehicleModel? Model { get; set; }

    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();

    public virtual ICollection<VehicleInspection> VehicleInspections { get; set; } = new List<VehicleInspection>();
}
