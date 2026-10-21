namespace UtilitiesPortal.Api.Models;

/// <summary>Read-only: one row per bill with customer and meter details (api.vw_CustomerBillSummary).</summary>
public class CustomerBillSummary
{
    public Guid BillId { get; set; }
    public Guid CustomerId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public Guid MeterId { get; set; }
    public string MeterNumber { get; set; } = string.Empty;
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal KwhUsed { get; set; }
    public decimal RatePerKwh { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public DateOnly? PaidDate { get; set; }
}
