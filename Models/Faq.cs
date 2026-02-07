using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Faq
{
    public int FaqId { get; set; }

    public string? Question { get; set; }

    public string? Answer { get; set; }

    public bool? IsActive { get; set; }
}
