using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilitiesPortal.Api.Auth;
using UtilitiesPortal.Api.DTOs;
using UtilitiesPortal.Api.Services;

namespace UtilitiesPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // every action needs a valid login token; some also need a specific role
public class MetersController : ControllerBase
{
    private readonly IMeterService _meters;
    private readonly IReadingService _readings;

    public MetersController(IMeterService meters, IReadingService readings)
    {
        _meters = meters;
        _readings = readings;
    }

    // GET api/meters/my : the logged-in customer's own meters
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<List<MeterDto>>> GetMine()
    {
        var customerId = User.GetCustomerId();
        if (customerId is null)
        {
            return Forbid();
        }

        return Ok(await _meters.GetMyMetersAsync(customerId.Value));
    }

    // GET api/meters : every meter (admin only)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<MeterDto>>> GetAll()
    {
        return Ok(await _meters.GetAllMetersAsync());
    }

    // POST api/meters : add a meter to a customer (admin only)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MeterDto>> Create(CreateMeterRequest request)
    {
        var meter = await _meters.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, meter);
    }

    // PUT api/meters/{meterId} : change number, status or owner (admin only)
    [HttpPut("{meterId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MeterDto>> Update(Guid meterId, UpdateMeterRequest request)
    {
        return Ok(await _meters.UpdateAsync(meterId, request));
    }

    // GET api/meters/{meterId}/readings : usage history of one meter (owner customer, or admin)
    [HttpGet("{meterId:guid}/readings")]
    public async Task<ActionResult<List<ReadingDto>>> GetReadings(Guid meterId)
    {
        var readings = await _readings.GetReadingsAsync(meterId, User.GetCustomerId(), User.IsAdmin());
        return Ok(readings);
    }
}
