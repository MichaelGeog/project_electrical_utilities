using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilitiesPortal.Api.DTOs;
using UtilitiesPortal.Api.Services;

namespace UtilitiesPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // any logged-in user (customer or admin) can see the outage panel
public class OutagesController : ControllerBase
{
    private readonly IOutageService _outages;

    public OutagesController(IOutageService outages)
    {
        _outages = outages;
    }

    // GET api/outages : simulated outage numbers per region
    [HttpGet]
    public async Task<ActionResult<List<OutageDto>>> GetAll()
    {
        return Ok(await _outages.GetAllAsync());
    }
}
