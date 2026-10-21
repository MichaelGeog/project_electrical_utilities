namespace UtilitiesPortal.Api.Models;

/// <summary>
/// Read-only: the most recent reading for each meter (api.vw_MeterLatestReading).
/// The reading fields are null for a meter that has no readings yet.
/// </summary>
public class MeterLatestReading
{
    public Guid MeterId { get; set; }
    public Guid CustomerId { get; set; }
    public string MeterNumber { get; set; } = string.Empty;
    public Guid? ReadingId { get; set; }
    public DateTime? ReadingDate { get; set; }
    public decimal? KwhReading { get; set; }
}
