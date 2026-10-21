namespace UtilitiesPortal.Api.Models;

/// <summary>Cumulative kWh reading for a meter. Maps to api.vw_MeterReadings.</summary>
public class MeterReading
{
    public Guid Id { get; set; }
    public Guid MeterId { get; set; }
    public DateTime ReadingDate { get; set; }
    public decimal KwhReading { get; set; }
}
