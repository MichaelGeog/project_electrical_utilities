using Microsoft.EntityFrameworkCore;
using UtilitiesPortal.Api.Data;
using UtilitiesPortal.Api.DTOs;
using UtilitiesPortal.Api.Errors;
using UtilitiesPortal.Api.Models;

namespace UtilitiesPortal.Api.Services;

/// <summary>The six regions the database allows (its CHECK constraint lists the same ones).</summary>
public static class ServiceRegions
{
    public static readonly string[] All =
        ["Bronx", "Brooklyn", "Manhattan", "Queens", "Staten Island", "Westchester"];
}

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllAsync();
    Task<CustomerDto> GetAsync(Guid customerId);

    /// <summary>Creates the login and the customer profile together, or neither.</summary>
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request);

    Task<CustomerDto> UpdateAsync(Guid customerId, UpdateCustomerRequest request);

    /// <summary>Soft delete: turns the customer's login off or on. Nothing is removed.</summary>
    Task<CustomerDto> SetActiveAsync(Guid customerId, bool isActive);
}

public class CustomerService : ICustomerService
{
    private readonly UtilitiesPortalContext _db;

    public CustomerService(UtilitiesPortalContext db)
    {
        _db = db;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        return await Query().OrderBy(c => c.AccountNumber).ToListAsync();
    }

    public async Task<CustomerDto> GetAsync(Guid customerId)
    {
        var customer = await Query().FirstOrDefaultAsync(c => c.CustomerId == customerId);
        return customer ?? throw new NotFoundException("Customer not found.");
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request)
    {
        // [Required] on the DTO already guarantees these have values.
        var email = request.Email!.Trim();
        var fullName = request.FullName!.Trim();
        var phone = request.Phone!.Trim();
        var address = request.ServiceAddress!.Trim();
        var region = CheckRegion(request.Region);

        if (await _db.AppUsers.AnyAsync(u => u.Email == email))
        {
            throw new ConflictException("A user with that email already exists.");
        }

        var accountNumber = await NextAccountNumberAsync();

        // Two inserts (the login, then the profile that points to it) that must succeed together.
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password!, workFactor: 12),
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            AccountNumber = accountNumber,
            FullName = fullName,
            Phone = phone,
            ServiceAddress = address,
            Region = region
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        return await GetAsync(customer.Id);
    }

    public async Task<CustomerDto> UpdateAsync(Guid customerId, UpdateCustomerRequest request)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId)
                       ?? throw new NotFoundException("Customer not found.");

        customer.FullName = request.FullName!.Trim();
        customer.Phone = request.Phone!.Trim();
        customer.ServiceAddress = request.ServiceAddress!.Trim();
        customer.Region = CheckRegion(request.Region);

        await _db.SaveChangesAsync();
        return await GetAsync(customerId);
    }

    public async Task<CustomerDto> SetActiveAsync(Guid customerId, bool isActive)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId)
                       ?? throw new NotFoundException("Customer not found.");

        var user = await _db.AppUsers.FirstAsync(u => u.Id == customer.UserId);
        user.IsActive = isActive;

        await _db.SaveChangesAsync();
        return await GetAsync(customerId);
    }

    // customers + their login + a count of their active meters, in one SQL query
    private IQueryable<CustomerDto> Query()
    {
        return from c in _db.Customers.AsNoTracking()
               join u in _db.AppUsers.AsNoTracking() on c.UserId equals u.Id
               select new CustomerDto
               {
                   CustomerId = c.Id,
                   AccountNumber = c.AccountNumber,
                   FullName = c.FullName,
                   Email = u.Email,
                   Phone = c.Phone,
                   ServiceAddress = c.ServiceAddress,
                   Region = c.Region,
                   IsActive = u.IsActive,
                   ActiveMeterCount = _db.Meters.Count(m => m.CustomerId == c.Id && m.Status == "Active")
               };
    }

    private static string CheckRegion(string? region)
    {
        var match = ServiceRegions.All.FirstOrDefault(r =>
            r.Equals(region?.Trim(), StringComparison.OrdinalIgnoreCase));

        return match ?? throw new BadRequestException(
            $"Region must be one of: {string.Join(", ", ServiceRegions.All)}.");
    }

    // CE-100001, CE-100002, ... one higher than the largest existing number.
    // (If two admins create at the exact same moment, the unique constraint rejects one.)
    private async Task<string> NextAccountNumberAsync()
    {
        var existing = await _db.Customers.AsNoTracking().Select(c => c.AccountNumber).ToListAsync();

        var highest = existing
            .Where(a => a.StartsWith("CE-", StringComparison.OrdinalIgnoreCase))
            .Select(a => int.TryParse(a.AsSpan(3), out var n) ? n : 0)
            .DefaultIfEmpty(100000)
            .Max();

        return $"CE-{highest + 1}";
    }
}
