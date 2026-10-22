using Microsoft.EntityFrameworkCore;
using UtilitiesPortal.Api.Data;
using UtilitiesPortal.Api.DTOs;

namespace UtilitiesPortal.Api.Services;

public interface IOutageService
{
    /// <summary>The simulated outage numbers for every region.</summary>
    Task<List<OutageDto>> GetAllAsync();
}

public class OutageService : IOutageService
{
    private readonly UtilitiesPortalContext _db;

    public OutageService(UtilitiesPortalContext db)
    {
        _db = db;
    }

    public async Task<List<OutageDto>> GetAllAsync()
    {
        var rows = await _db.OutageStatuses
            .AsNoTracking()
            .OrderBy(o => o.Region)
            .ToListAsync();

        return rows.Select(o => new OutageDto
        {
            Region = o.Region,
            CustomersTracked = o.CustomersTracked,
            CustomersOut = o.CustomersOut,
            // Worked out here (not in SQL) so a region with 0 tracked customers can't divide by zero.
            PercentOut = o.CustomersTracked == 0
                ? 0
                : Math.Round(o.CustomersOut * 100m / o.CustomersTracked, 2, MidpointRounding.AwayFromZero),
            UpdatedAt = DateTime.SpecifyKind(o.UpdatedAt, DateTimeKind.Utc)
        }).ToList();
    }
}
