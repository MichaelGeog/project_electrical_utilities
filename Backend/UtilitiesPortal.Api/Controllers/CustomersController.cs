using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilitiesPortal.Api.DTOs;
using UtilitiesPortal.Api.Services;

namespace UtilitiesPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // customers are managed by admins only
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers)
    {
        _customers = customers;
    }

    // GET api/customers
    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll()
    {
        return Ok(await _customers.GetAllAsync());
    }

    // GET api/customers/{customerId}
    [HttpGet("{customerId:guid}")]
    public async Task<ActionResult<CustomerDto>> Get(Guid customerId)
    {
        return Ok(await _customers.GetAsync(customerId));
    }

    // POST api/customers : create a customer and their login
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request)
    {
        var customer = await _customers.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, customer);
    }

    // PUT api/customers/{customerId} : edit name, phone, address, region
    [HttpPut("{customerId:guid}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid customerId, UpdateCustomerRequest request)
    {
        return Ok(await _customers.UpdateAsync(customerId, request));
    }

    // PUT api/customers/{customerId}/active : deactivate or reactivate (soft delete)
    [HttpPut("{customerId:guid}/active")]
    public async Task<ActionResult<CustomerDto>> SetActive(Guid customerId, SetActiveRequest request)
    {
        return Ok(await _customers.SetActiveAsync(customerId, request.IsActive!.Value));
    }
}
