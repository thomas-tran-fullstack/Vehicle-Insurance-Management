using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class BillPayment
{
    public int BillPaymentId { get; set; }

    public int BillId { get; set; }

    public decimal Amount { get; set; }

    public string? Method { get; set; }

    public string? Status { get; set; }

    public string? TransactionRef { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public virtual Bill? Bill { get; set; }
}
