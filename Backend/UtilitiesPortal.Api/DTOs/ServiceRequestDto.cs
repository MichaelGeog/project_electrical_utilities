namespace UtilitiesPortal.Api.DTOs;

/// <summary>A customer's service request, with who raised it.</summary>
public class ServiceRequestDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Outage, Billing, Meter Issue or Other.</summary>
    public string Type { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Open, In Progress or Resolved.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
