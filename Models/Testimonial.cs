using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Testimonial
{
    public int TestimonialId { get; set; }

    public int? CustomerId { get; set; }

    public string? Content { get; set; }

    public int? Rating { get; set; }

    public string Status { get; set; } = "Pending"; // Published, Pending, Denied

    public DateTime? CreatedDate { get; set; } 

    public virtual Customer? Customer { get; set; }
}
