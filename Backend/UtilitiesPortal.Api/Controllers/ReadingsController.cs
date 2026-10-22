using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilitiesPortal.Api.DTOs;
using UtilitiesPortal.Api.Services;

namespace UtilitiesPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // only admins record readings
public class ReadingsController : ControllerBase
{
    private readonly IReadingService _readings;

    public ReadingsController(IReadingService readings)
    {
        _readings = readings;
    }

    // POST api/readings : record a new meter reading
    [HttpPost]
    public async Task<ActionResult<ReadingDto>> Create(CreateReadingRequest request)
    {
        var created = await _readings.CreateAsync(request);
        return Created($"/api/meters/{created.MeterId}/readings", created);
    }
}
