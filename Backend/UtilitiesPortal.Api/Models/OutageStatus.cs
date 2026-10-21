namespace UtilitiesPortal.Api.Models;

/// <summary>Simulated outage numbers per service region (Tier 3). Maps to api.vw_OutageStatus.</summary>
public class OutageStatus
{
    public Guid Id { get; set; }
    public string Region { get; set; } = string.Empty;
    public int CustomersTracked { get; set; }
    public int CustomersOut { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
