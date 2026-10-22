using System.ComponentModel.DataAnnotations;

namespace UtilitiesPortal.Api.DTOs;

public class UpdateServiceRequestStatusRequest
{
    /// <summary>Open, In Progress or Resolved.</summary>
    [Required(ErrorMessage = "Status is required.")]
    public string? Status { get; set; }
}
