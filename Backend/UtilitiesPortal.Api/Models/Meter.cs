namespace UtilitiesPortal.Api.Models;

/// <summary>Electric meter at a customer's service address. Maps to api.vw_Meters.</summary>
public class Meter
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string MeterNumber { get; set; } = string.Empty;

    /// <summary>Active or Inactive (Inactive is the soft delete).</summary>
    public string Status { get; set; } = "Active";

    public DateOnly InstalledDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}
