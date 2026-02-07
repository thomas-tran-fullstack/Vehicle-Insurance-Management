using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class CompanyExpense
{
    public int ExpenseId { get; set; }

    public DateOnly? ExpenseDate { get; set; }

    public string? ExpenseType { get; set; }

    public string? Description { get; set; }

    public decimal? Amount { get; set; }
}
