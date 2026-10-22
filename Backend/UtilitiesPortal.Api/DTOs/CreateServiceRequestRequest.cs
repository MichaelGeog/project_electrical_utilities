using System.ComponentModel.DataAnnotations;

namespace UtilitiesPortal.Api.DTOs;

/// <summary>
/// What a customer sends to open a request. There is no customer id here: the server takes it
/// from the login token, so nobody can file a request as someone else.
/// </summary>
public class CreateServiceRequestRequest
{
    [Required(ErrorMessage = "Type is required.")]
    public string? Type { get; set; }

    [Required(ErrorMessage = "Please describe the problem.")]
    [StringLength(1000, ErrorMessage = "The description can be at most 1000 characters.")]
    public string? Description { get; set; }
}
