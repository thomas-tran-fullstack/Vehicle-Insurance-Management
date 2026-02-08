using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Bill
{
    public int BillId { get; set; }

    public int? PolicyId { get; set; }

    public int? CustomerId { get; set; }

    public long? PolicyNumberSnapshot { get; set; }

    public string? CustomerNameSnapshot { get; set; }

    public string? CustomerPhoneSnapshot { get; set; }

    public string? CustomerAddressProofSnapshot { get; set; }

    public string? VehicleNameSnapshot { get; set; }

    public string? VehicleModelSnapshot { get; set; }

    public decimal? VehicleRateSnapshot { get; set; }

    public string? VehicleBodyNumberSnapshot { get; set; }

    public string? VehicleEngineNumberSnapshot { get; set; }

    public DateOnly? BillDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public string? BillType { get; set; }

    public decimal? Amount { get; set; }

    public bool? Paid { get; set; }

    public string? Status { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual Policy? Policy { get; set; }

    public virtual ICollection<BillPayment> BillPayments { get; set; } = new List<BillPayment>();
}
