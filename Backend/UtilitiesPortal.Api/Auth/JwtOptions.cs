namespace UtilitiesPortal.Api.Auth;

/// <summary>
/// Settings for creating and checking login tokens, bound from the "Jwt" section of configuration.
/// Issuer, Audience and ExpiryMinutes live in appsettings.json. Key is a secret: it lives in
/// user secrets locally and in the App Service configuration on Azure, never in git.
/// </summary>
public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
