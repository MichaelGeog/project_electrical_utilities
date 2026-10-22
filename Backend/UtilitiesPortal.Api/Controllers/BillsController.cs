using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilitiesPortal.Api.Auth;
using UtilitiesPortal.Api.DTOs;
using UtilitiesPortal.Api.Services;

namespace UtilitiesPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // every action needs a valid login token; each one also names the role it allows
public class BillsController : ControllerBase
{
    private readonly IBillingService _bills;

    public BillsController(IBillingService bills)
    {
        _bills = bills;
    }

    // GET api/bills/my : the logged-in customer's own bills
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<List<BillDto>>> GetMine()
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
        {
            return Forbid();
        }

        return Ok(await _bills.GetMyBillsAsync(customerId.Value));
    }

    // GET api/bills?status=Unpaid : every bill, optionally filtered (admin only)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<BillDto>>> GetAll([FromQuery] string? status)
    {
        return Ok(await _bills.GetAllBillsAsync(status));
    }

    // POST api/bills/generate : create a bill for a meter and period (admin only)
    [HttpPost("generate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BillDto>> Generate(GenerateBillRequest request)
    {
        var bill = await _bills.GenerateAsync(request);
        return StatusCode(StatusCodes.Status201Created, bill);
    }
}
