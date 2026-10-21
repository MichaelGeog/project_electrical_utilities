namespace UtilitiesPortal.Api.Models;

/// <summary>Customer service or outage request (Tier 2). Maps to api.vw_ServiceRequests.</summary>
public class ServiceRequest
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }

    /// <summary>Outage, Billing, Meter Issue or Other.</summary>
    public string Type { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Open, In Progress or Resolved.</summary>
    public string Status { get; set; } = "Open";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The database does not update this on its own: set it whenever the request changes.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
