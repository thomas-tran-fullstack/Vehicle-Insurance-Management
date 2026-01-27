using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Testimonial
{
    public int TestimonialId { get; set; }

    public int? CustomerId { get; set; }

    public string? Content { get; set; }

    public bool? Approved { get; set; }

    public virtual Customer? Customer { get; set; }
}
