using Microsoft.EntityFrameworkCore;
using UtilitiesPortal.Api.Data;
using UtilitiesPortal.Api.DTOs;
using UtilitiesPortal.Api.Errors;
using UtilitiesPortal.Api.Models;

namespace UtilitiesPortal.Api.Services;

/// <summary>The allowed values (the database CHECK constraints list the same ones).</summary>
public static class ServiceRequestRules
{
    public static readonly string[] Types = ["Outage", "Billing", "Meter Issue", "Other"];
    public static readonly string[] Statuses = ["Open", "In Progress", "Resolved"];

    /// <summary>Matches a value ignoring case and returns the official spelling, or null.</summary>
    public static string? Canonical(string[] allowed, string? value)
    {
        return allowed.FirstOrDefault(a => a.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

public interface IServiceRequestService
{
    /// <summary>A customer opens a new request (status Open).</summary>
    Task<ServiceRequestDto> CreateAsync(Guid customerId, CreateServiceRequestRequest request);

    /// <summary>The requests of one customer, newest first.</summary>
    Task<List<ServiceRequestDto>> GetMineAsync(Guid customerId);

    /// <summary>All requests (admin), optionally only one status.</summary>
    Task<List<ServiceRequestDto>> GetAllAsync(string? status);

    /// <summary>Admin moves a request to Open, In Progress or Resolved.</summary>
    Task<ServiceRequestDto> UpdateStatusAsync(Guid requestId, string status);
}

public class ServiceRequestService : IServiceRequestService
{
    private readonly UtilitiesPortalContext _db;

    public ServiceRequestService(UtilitiesPortalContext db)
    {
        _db = db;
    }

    public async Task<ServiceRequestDto> CreateAsync(Guid customerId, CreateServiceRequestRequest request)
    {
        var type = ServiceRequestRules.Canonical(ServiceRequestRules.Types, request.Type)
                   ?? throw new BadRequestException(
                       $"Type must be one of: {string.Join(", ", ServiceRequestRules.Types)}.");

        var description = request.Description!.Trim();

        var now = DateTime.UtcNow;
        var entity = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Type = type,
            Description = description,
            Status = "Open",
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.ServiceRequests.Add(entity);
        await _db.SaveChangesAsync();

        return await GetOneAsync(entity.Id);
    }

    public async Task<List<ServiceRequestDto>> GetMineAsync(Guid customerId)
    {
        var list = await Query()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return MarkUtc(list);
    }

    public async Task<List<ServiceRequestDto>> GetAllAsync(string? status)
    {
        string? wanted = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            wanted = ServiceRequestRules.Canonical(ServiceRequestRules.Statuses, status)
                     ?? throw new BadRequestException("Status must be Open, In Progress or Resolved.");
        }

        var query = Query();
        if (wanted is not null)
        {
            query = query.Where(r => r.Status == wanted);
        }

        var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return MarkUtc(list);
    }

    public async Task<ServiceRequestDto> UpdateStatusAsync(Guid requestId, string status)
    {
        var newStatus = ServiceRequestRules.Canonical(ServiceRequestRules.Statuses, status)
                        ?? throw new BadRequestException("Status must be Open, In Progress or Resolved.");

        var entity = await _db.ServiceRequests.FirstOrDefaultAsync(r => r.Id == requestId)
                     ?? throw new NotFoundException("Service request not found.");

        if (entity.Status != newStatus)
        {
            entity.Status = newStatus;
            // The database doesn't touch UpdatedAt by itself, so we set it on every change.
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return await GetOneAsync(requestId);
    }

    private async Task<ServiceRequestDto> GetOneAsync(Guid requestId)
    {
        var dto = await Query().FirstOrDefaultAsync(r => r.Id == requestId)
                  ?? throw new NotFoundException("Service request not found.");
        return MarkUtc([dto])[0];
    }

    // requests + the customer who raised each one, in one SQL query
    private IQueryable<ServiceRequestDto> Query()
    {
        return from r in _db.ServiceRequests.AsNoTracking()
               join c in _db.Customers.AsNoTracking() on r.CustomerId equals c.Id
               select new ServiceRequestDto
               {
                   Id = r.Id,
                   CustomerId = c.Id,
                   AccountNumber = c.AccountNumber,
                   CustomerName = c.FullName,
                   Type = r.Type,
                   Description = r.Description,
                   Status = r.Status,
                   CreatedAt = r.CreatedAt,
                   UpdatedAt = r.UpdatedAt
               };
    }

    // datetime2 comes back with no time zone marker; mark it UTC so the JSON ends with "Z".
    private static List<ServiceRequestDto> MarkUtc(List<ServiceRequestDto> list)
    {
        foreach (var dto in list)
        {
            dto.CreatedAt = DateTime.SpecifyKind(dto.CreatedAt, DateTimeKind.Utc);
            dto.UpdatedAt = DateTime.SpecifyKind(dto.UpdatedAt, DateTimeKind.Utc);
        }
        return list;
    }
}
