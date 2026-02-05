using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Vehicle
{
    public int VehicleId { get; set; }

    public int? CustomerId { get; set; }

    public int? ModelId { get; set; }

    public string? VehicleName { get; set; }

    public string? VehicleType { get; set; }

    public string? VehicleBrand { get; set; }

    public string? VehicleSegment { get; set; }

    public string? VehicleVersion { get; set; }

    public decimal? VehicleRate { get; set; }

    public string? BodyNumber { get; set; }

    public string? EngineNumber { get; set; }

    public string? VehicleNumber { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public int? SeatCount { get; set; }

    public string? VehicleImage { get; set; }

    public int? ManufactureYear { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? VehicleOwnerName { get; set; }

    public string? VehicleModelName { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Estimate> Estimates { get; set; } = new List<Estimate>();

    public virtual VehicleModel? Model { get; set; }

    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();

    public virtual ICollection<VehicleInspection> VehicleInspections { get; set; } = new List<VehicleInspection>();
}
