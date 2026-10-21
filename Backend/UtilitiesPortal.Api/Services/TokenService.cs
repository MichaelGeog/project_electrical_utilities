using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using UtilitiesPortal.Api.Auth;
using UtilitiesPortal.Api.Models;

namespace UtilitiesPortal.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(AppUser user, Guid? customerId);
}

/// <summary>Creates the signed login token (JWT) that the Angular app sends with every request.</summary>
public class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(AppUser user, Guid? customerId)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var claims = new Dictionary<string, object>
        {
            [AppClaims.Subject] = user.Id.ToString(),
            [AppClaims.Email] = user.Email,
            [AppClaims.Role] = user.Role
        };
        if (customerId is not null)
        {
            claims[AppClaims.CustomerId] = customerId.Value.ToString();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expires,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expires);
    }
}
