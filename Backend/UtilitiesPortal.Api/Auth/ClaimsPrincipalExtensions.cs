using System.Security.Claims;

namespace UtilitiesPortal.Api.Auth;

/// <summary>Small helpers so controllers can ask "who is calling?" in one readable line.</summary>
public static class ClaimsPrincipalExtensions
{
    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("Admin");
    }

    /// <summary>The customer Id from the login token, or null (admins have none).</summary>
    public static Guid? GetCustomerId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(AppClaims.CustomerId)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
