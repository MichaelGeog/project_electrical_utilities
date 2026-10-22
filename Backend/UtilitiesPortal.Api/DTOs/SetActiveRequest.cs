using System.ComponentModel.DataAnnotations;

namespace UtilitiesPortal.Api.DTOs;

/// <summary>Deactivate (false) or reactivate (true) a customer's login.</summary>
public class SetActiveRequest
{
    [Required(ErrorMessage = "IsActive is required.")]
    public bool? IsActive { get; set; }
}
