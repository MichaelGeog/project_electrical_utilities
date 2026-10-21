using Microsoft.EntityFrameworkCore;
using UtilitiesPortal.Api.Data;
using UtilitiesPortal.Api.DTOs;

namespace UtilitiesPortal.Api.Services;

public interface IAuthService
{
    /// <summary>Returns the login result, or null when the email or password is wrong.</summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}

public class AuthService : IAuthService
{
    private readonly UtilitiesPortalContext _db;
    private readonly ITokenService _tokens;

    public AuthService(UtilitiesPortalContext db, ITokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim();

        var user = await _db.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);

        // One check for every failure, so the response never reveals whether the email exists.
        if (user is null
            || !user.IsActive
            || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        Guid? customerId = null;
        if (user.Role == "Customer")
        {
            customerId = await _db.Customers
                .AsNoTracking()
                .Where(c => c.UserId == user.Id)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync();
        }

        var (token, expiresAt) = _tokens.CreateToken(user, customerId);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email,
            Role = user.Role,
            CustomerId = customerId
        };
    }
}
