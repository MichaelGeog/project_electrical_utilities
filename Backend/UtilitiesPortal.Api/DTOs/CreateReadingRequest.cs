using System.ComponentModel.DataAnnotations;

namespace UtilitiesPortal.Api.DTOs;

/// <summary>
/// What the admin form sends to record a reading. The properties are nullable on purpose:
/// with [Required], a missing value is reported as missing instead of silently becoming
/// an empty Guid or a 0 reading.
/// </summary>
public class CreateReadingRequest
{
    [Required(ErrorMessage = "Meter is required.")]
    public Guid? MeterId { get; set; }

    [Required(ErrorMessage = "Reading date is required.")]
    public DateTime? ReadingDate { get; set; }

    [Required(ErrorMessage = "The reading is required.")]
    [Range(0, 9999999999.99, ErrorMessage = "The reading must be 0 or more.")]
    public decimal? KwhReading { get; set; }
}
