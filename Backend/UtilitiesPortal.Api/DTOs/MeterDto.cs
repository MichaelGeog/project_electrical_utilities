namespace UtilitiesPortal.Api.DTOs;

/// <summary>A meter with its customer and its most recent reading, as the client sees it.</summary>
public class MeterDto
{
    public Guid MeterId { get; set; }
    public string MeterNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly InstalledDate { get; set; }

    public Guid CustomerId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ServiceAddress { get; set; } = string.Empty;

    /// <summary>Null when the meter has no readings yet.</summary>
    public DateTime? LatestReadingDate { get; set; }
    public decimal? LatestKwhReading { get; set; }
}
