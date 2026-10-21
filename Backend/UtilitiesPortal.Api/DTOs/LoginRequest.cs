using System.ComponentModel.DataAnnotations;

namespace UtilitiesPortal.Api.DTOs;

/// <summary>What the Angular login form sends. Checked here before it reaches the database.</summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;
}
