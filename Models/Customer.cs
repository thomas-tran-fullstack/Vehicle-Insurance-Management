using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public int? UserId { get; set; }

    public string? CustomerName { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Avatar { get; set; }

    public virtual ICollection<Estimate> Estimates { get; set; } = new List<Estimate>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();

    public virtual ICollection<Testimonial> Testimonials { get; set; } = new List<Testimonial>();

    public virtual User? User { get; set; }

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
