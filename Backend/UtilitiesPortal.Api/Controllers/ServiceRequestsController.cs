using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilitiesPortal.Api.Auth;
using UtilitiesPortal.Api.DTOs;
using UtilitiesPortal.Api.Services;

namespace UtilitiesPortal.Api.Controllers;

[ApiController]
[Route("api/service-requests")]
[Authorize] // every action needs a valid login token; each one also names the role it allows
public class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestService _requests;

    public ServiceRequestsController(IServiceRequestService requests)
    {
        _requests = requests;
    }

    // POST api/service-requests : a customer opens a request
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ServiceRequestDto>> Create(CreateServiceRequestRequest request)
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
        {
            return Forbid();
        }

        var created = await _requests.CreateAsync(customerId.Value, request);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    // GET api/service-requests/my : the logged-in customer's own requests
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<List<ServiceRequestDto>>> GetMine()
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
        {
            return Forbid();
        }

        return Ok(await _requests.GetMineAsync(customerId.Value));
    }

    // GET api/service-requests?status=Open : every request (admin only)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ServiceRequestDto>>> GetAll([FromQuery] string? status)
    {
        return Ok(await _requests.GetAllAsync(status));
    }

    // PUT api/service-requests/{id}/status : admin updates the status
    [HttpPut("{requestId:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ServiceRequestDto>> UpdateStatus(
        Guid requestId, UpdateServiceRequestStatusRequest request)
    {
        return Ok(await _requests.UpdateStatusAsync(requestId, request.Status!));
    }
}
