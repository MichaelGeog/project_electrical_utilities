namespace UtilitiesPortal.Api.Models;

/// <summary>Bill for one meter and one billing period. Maps to api.vw_Bills.</summary>
public class Bill
{
    public Guid Id { get; set; }
    public Guid MeterId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal KwhUsed { get; set; }
    public decimal RatePerKwh { get; set; }

    /// <summary>Calculated by the backend: KwhUsed x RatePerKwh, rounded away from zero to 2 decimals.</summary>
    public decimal Amount { get; set; }

    /// <summary>Unpaid or Paid. A Paid bill must have a PaidDate.</summary>
    public string Status { get; set; } = "Unpaid";

    public DateOnly DueDate { get; set; }
    public DateOnly? PaidDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
