namespace UtilitiesPortal.Api.DTOs;

/// <summary>What the API returns after a successful login. Never includes the password hash.</summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    /// <summary>Set for customers, null for admins.</summary>
    public Guid? CustomerId { get; set; }
}
