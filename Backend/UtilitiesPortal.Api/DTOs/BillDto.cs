namespace UtilitiesPortal.Api.DTOs;

/// <summary>A bill with its customer and meter details, as the client sees it.</summary>
public class BillDto
{
    public Guid BillId { get; set; }

    public Guid CustomerId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    public Guid MeterId { get; set; }
    public string MeterNumber { get; set; } = string.Empty;

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal KwhUsed { get; set; }
    public decimal RatePerKwh { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Unpaid or Paid.</summary>
    public string Status { get; set; } = string.Empty;

    public DateOnly DueDate { get; set; }
    public DateOnly? PaidDate { get; set; }
}
