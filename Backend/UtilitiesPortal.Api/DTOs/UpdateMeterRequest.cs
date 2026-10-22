using System.ComponentModel.DataAnnotations;

namespace UtilitiesPortal.Api.DTOs;

/// <summary>The full set of fields an admin may change on a meter (including its owner).</summary>
public class UpdateMeterRequest
{
    [Required(ErrorMessage = "Customer is required.")]
    public Guid? CustomerId { get; set; }

    [Required(ErrorMessage = "Meter number is required.")]
    [StringLength(20, ErrorMessage = "Meter number can be at most 20 characters.")]
    public string? MeterNumber { get; set; }

    /// <summary>Active or Inactive. Inactive is the soft delete.</summary>
    [Required(ErrorMessage = "Status is required.")]
    public string? Status { get; set; }
}
