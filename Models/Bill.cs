using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Bill
{
    public int BillId { get; set; }

    public int? PolicyId { get; set; }

    public DateOnly? BillDate { get; set; }

    public decimal? Amount { get; set; }

    public bool? Paid { get; set; }

    public virtual Policy? Policy { get; set; }
}
