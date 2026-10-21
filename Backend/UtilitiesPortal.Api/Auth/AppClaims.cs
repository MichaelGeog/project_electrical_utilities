namespace UtilitiesPortal.Api.Auth;

/// <summary>
/// Claim names used inside the login token. Short names keep the token small and easy to read
/// from the Angular app (for example, the "role" claim decides which screens to show).
/// </summary>
public static class AppClaims
{
    public const string Subject = "sub";          // the user's Id
    public const string Email = "email";
    public const string Role = "role";            // "Customer" or "Admin"
    public const string CustomerId = "customerId"; // only present for customers
}
