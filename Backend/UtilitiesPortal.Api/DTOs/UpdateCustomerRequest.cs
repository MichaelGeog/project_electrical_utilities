using System.ComponentModel.DataAnnotations;

namespace UtilitiesPortal.Api.DTOs;

/// <summary>The profile fields an admin may change. Email and account number are fixed.</summary>
public class UpdateCustomerRequest
{
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
