using System;
using System.Collections.Generic;

namespace VehicleInsuranceAPI.Models;

public partial class Contact
{
    public int ContactId { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Message { get; set; }

    public string? Subject { get; set; }

    public int? CategoryId { get; set; }

    public int? UserId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? Status { get; set; } = "Open"; // "Open" or "Resolved"

    public virtual ContactCategory? Category { get; set; }
}