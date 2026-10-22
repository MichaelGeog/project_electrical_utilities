namespace UtilitiesPortal.Api.DTOs;

/// <summary>One meter reading, plus how much was used since the reading before it.</summary>
public class ReadingDto
{
    public Guid Id { get; set; }
    public Guid MeterId { get; set; }
    public DateTime ReadingDate { get; set; }

    /// <summary>The cumulative number on the meter.</summary>
    public decimal KwhReading { get; set; }

    /// <summary>This reading minus the previous one. Null for a meter's very first reading.</summary>
    public decimal? KwhUsed { get; set; }
}
