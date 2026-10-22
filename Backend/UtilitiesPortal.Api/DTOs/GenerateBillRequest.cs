using System.ComponentModel.DataAnnotations;

namespace UtilitiesPortal.Api.DTOs;

/// <summary>
/// What the admin sends to bill a meter for a period. There is deliberately no amount, usage
/// or rate here: the server works those out from the readings, so a client can't set its own price.
/// </summary>
public class GenerateBillRequest
{
    [Required(ErrorMessage = "Meter is required.")]
    public Guid? MeterId { get; set; }

    [Required(ErrorMessage = "Period start is required.")]
    public DateOnly? PeriodStart { get; set; }

    [Required(ErrorMessage = "Period end is required.")]
    public DateOnly? PeriodEnd { get; set; }
}
