using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilitiesPortal.Api.Auth;
using UtilitiesPortal.Api.DTOs;
using UtilitiesPortal.Api.Services;

namespace UtilitiesPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    // POST api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        if (result is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid email or password."
            });
        }

        return Ok(result);
    }

    // GET api/auth/me : who is the logged-in user (the Angular app can use this on startup)
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirst(AppClaims.Subject)?.Value,
            email = User.FindFirst(AppClaims.Email)?.Value,
            role = User.FindFirst(AppClaims.Role)?.Value,
            customerId = User.FindFirst(AppClaims.CustomerId)?.Value
        });
    }

    // GET api/auth/admin-only : TEMPORARY, proves the Admin role check works. Delete it later.
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "You are an admin." });
    }
}
