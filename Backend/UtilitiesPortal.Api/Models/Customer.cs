namespace UtilitiesPortal.Api.Models;

/// <summary>Customer profile linked one-to-one to a user. Maps to api.vw_Customers.</summary>
public class Customer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ServiceAddress { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}
