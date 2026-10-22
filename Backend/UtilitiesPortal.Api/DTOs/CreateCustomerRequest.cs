using System.ComponentModel.DataAnnotations;

namespace UtilitiesPortal.Api.DTOs;

/// <summary>
/// What the admin sends to create a customer together with their login.
/// The account number is not here: the server assigns the next one.
/// </summary>
public class CreateCustomerRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256, ErrorMessage = "Email can be at most 256 characters.")]
    public string? Email { get; set; }

    /// <summary>A temporary password the admin chooses. It is stored only as a BCrypt hash.</summary>
    [Required(ErrorMessage = "A temporary password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be 8 to 100 characters.")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name can be at most 100 characters.")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits.")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Service address is required.")]
    [StringLength(200, ErrorMessage = "Service address can be at most 200 characters.")]
    public string? ServiceAddress { get; set; }

    [Required(ErrorMessage = "Region is required.")]
    public string? Region { get; set; }
}
