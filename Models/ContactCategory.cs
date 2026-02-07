using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class ContactCategory
{
    public int CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}
