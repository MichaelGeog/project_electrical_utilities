using System.ComponentModel.DataAnnotations;

namespace UtilitiesPortal.Api.DTOs;

public class CreateMeterRequest
{
    [Required(ErrorMessage = "Customer is required.")]
    public Guid? CustomerId { get; set; }

    [Required(ErrorMessage = "Meter number is required.")]
    [StringLength(20, ErrorMessage = "Meter number can be at most 20 characters.")]
    public string? MeterNumber { get; set; }

    /// <summary>Optional. Defaults to today.</summary>
    public DateOnly? InstalledDate { get; set; }
}
