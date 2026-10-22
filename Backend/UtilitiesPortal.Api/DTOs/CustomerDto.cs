namespace UtilitiesPortal.Api.DTOs;

/// <summary>A customer with their login details (never the password) as the admin sees them.</summary>
public class CustomerDto
{
    public Guid CustomerId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ServiceAddress { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;

    /// <summary>False when the customer has been deactivated (they can no longer log in).</summary>
    public bool IsActive { get; set; }

    public int ActiveMeterCount { get; set; }
}
