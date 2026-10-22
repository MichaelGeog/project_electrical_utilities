namespace UtilitiesPortal.Api.DTOs;

/// <summary>Simulated outage numbers for one service region.</summary>
public class OutageDto
{
    public string Region { get; set; } = string.Empty;
    public int CustomersTracked { get; set; }
    public int CustomersOut { get; set; }

    /// <summary>CustomersOut as a percentage of CustomersTracked, to 2 decimals.</summary>
    public decimal PercentOut { get; set; }

    public DateTime UpdatedAt { get; set; }
}
