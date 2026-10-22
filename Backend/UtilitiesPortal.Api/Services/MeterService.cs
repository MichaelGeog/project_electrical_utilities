using Microsoft.EntityFrameworkCore;
using UtilitiesPortal.Api.Data;
using UtilitiesPortal.Api.DTOs;

namespace UtilitiesPortal.Api.Services;

public interface IMeterService
{
    /// <summary>The active meters of one customer.</summary>
    Task<List<MeterDto>> GetMyMetersAsync(Guid customerId);

    /// <summary>Every meter of every customer (admin screens).</summary>
    Task<List<MeterDto>> GetAllMetersAsync();
}

public class MeterService : IMeterService
{
    private readonly UtilitiesPortalContext _db;

    public MeterService(UtilitiesPortalContext db)
    {
        _db = db;
    }

    public Task<List<MeterDto>> GetMyMetersAsync(Guid customerId)
    {
        return LoadAsync(customerId, onlyActive: true);
    }

    public Task<List<MeterDto>> GetAllMetersAsync()
    {
        return LoadAsync(customerId: null, onlyActive: false);
    }

    private async Task<List<MeterDto>> LoadAsync(Guid? customerId, bool onlyActive)
    {
        var meters = _db.Meters.AsNoTracking().AsQueryable();

        if (customerId is not null)
        {
            var id = customerId.Value;
            meters = meters.Where(m => m.CustomerId == id);
        }

        if (onlyActive)
        {
            meters = meters.Where(m => m.Status == "Active");
        }

        // meters + their customer + their latest reading, in one SQL query.
        // The latest reading is a LEFT join, so a meter with no readings still appears.
        var query =
            from m in meters
            join c in _db.Customers.AsNoTracking() on m.CustomerId equals c.Id
            join l in _db.MeterLatestReadings.AsNoTracking() on m.Id equals l.MeterId into latest
            from l in latest.DefaultIfEmpty()
            orderby m.MeterNumber
            select new MeterDto
            {
                MeterId = m.Id,
                MeterNumber = m.MeterNumber,
                Status = m.Status,
                InstalledDate = m.InstalledDate,
                CustomerId = c.Id,
                AccountNumber = c.AccountNumber,
                CustomerName = c.FullName,
                ServiceAddress = c.ServiceAddress,
                LatestReadingDate = l.ReadingDate,
                LatestKwhReading = l.KwhReading
            };

        var result = await query.ToListAsync();

        // The database stores UTC, but datetime2 comes back with no time zone marker.
        // Marking it UTC makes the JSON end with "Z", so Angular shows the right local time.
        foreach (var dto in result)
        {
            if (dto.LatestReadingDate is not null)
            {
                dto.LatestReadingDate = DateTime.SpecifyKind(dto.LatestReadingDate.Value, DateTimeKind.Utc);
            }
        }

        return result;
    }
}
