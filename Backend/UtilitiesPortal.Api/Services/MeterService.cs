using Microsoft.EntityFrameworkCore;
using UtilitiesPortal.Api.Data;
using UtilitiesPortal.Api.DTOs;
using UtilitiesPortal.Api.Errors;
using UtilitiesPortal.Api.Models;

namespace UtilitiesPortal.Api.Services;

public interface IMeterService
{
    /// <summary>The active meters of one customer.</summary>
    Task<List<MeterDto>> GetMyMetersAsync(Guid customerId);

    /// <summary>Every meter of every customer (admin screens).</summary>
    Task<List<MeterDto>> GetAllMetersAsync();

    /// <summary>Adds a new active meter to a customer.</summary>
    Task<MeterDto> CreateAsync(CreateMeterRequest request);

    /// <summary>Changes a meter's number, status (Active/Inactive) or owner.</summary>
    Task<MeterDto> UpdateAsync(Guid meterId, UpdateMeterRequest request);
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

    public async Task<MeterDto> CreateAsync(CreateMeterRequest request)
    {
        var customerId = request.CustomerId!.Value;
        var meterNumber = request.MeterNumber!.Trim();
        var installed = request.InstalledDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (installed > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new BadRequestException("The installed date cannot be in the future.");
        }

        await EnsureCustomerCanOwnMetersAsync(customerId);

        if (await _db.Meters.AnyAsync(m => m.MeterNumber == meterNumber))
        {
            throw new ConflictException("A meter with that number already exists.");
        }

        var meter = new Meter
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            MeterNumber = meterNumber,
            Status = "Active",
            InstalledDate = installed
        };
        _db.Meters.Add(meter);
        await _db.SaveChangesAsync();

        return await GetOneAsync(meter.Id);
    }

    public async Task<MeterDto> UpdateAsync(Guid meterId, UpdateMeterRequest request)
    {
        var meter = await _db.Meters.FirstOrDefaultAsync(m => m.Id == meterId)
                    ?? throw new NotFoundException("Meter not found.");

        var newCustomerId = request.CustomerId!.Value;
        var newNumber = request.MeterNumber!.Trim();
        var newStatus = request.Status!.Trim();

        if (newStatus is not ("Active" or "Inactive"))
        {
            throw new BadRequestException("Status must be Active or Inactive.");
        }

        if (newNumber != meter.MeterNumber
            && await _db.Meters.AnyAsync(m => m.MeterNumber == newNumber && m.Id != meterId))
        {
            throw new ConflictException("A meter with that number already exists.");
        }

        if (newCustomerId != meter.CustomerId)
        {
            await EnsureCustomerCanOwnMetersAsync(newCustomerId);

            // Moving a meter would also move its bills to the new customer. To keep each
            // customer's billing history correct, only a meter without bills can change owner.
            if (await _db.Bills.AnyAsync(b => b.MeterId == meterId))
            {
                throw new BadRequestException(
                    "This meter already has bills, so it can't move to another customer. " +
                    "Set it to Inactive and add a new meter for the new customer instead.");
            }
        }

        meter.CustomerId = newCustomerId;
        meter.MeterNumber = newNumber;
        meter.Status = newStatus;

        await _db.SaveChangesAsync();
        return await GetOneAsync(meterId);
    }

    private async Task EnsureCustomerCanOwnMetersAsync(Guid customerId)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId)
                       ?? throw new NotFoundException("Customer not found.");

        var isActive = await _db.AppUsers.AnyAsync(u => u.Id == customer.UserId && u.IsActive);
        if (!isActive)
        {
            throw new BadRequestException("That customer is deactivated. Reactivate them first.");
        }
    }

    private async Task<MeterDto> GetOneAsync(Guid meterId)
    {
        var list = await LoadAsync(customerId: null, onlyActive: false, meterId: meterId);
        return list.FirstOrDefault() ?? throw new NotFoundException("Meter not found.");
    }

    private async Task<List<MeterDto>> LoadAsync(Guid? customerId, bool onlyActive, Guid? meterId = null)
    {
        var meters = _db.Meters.AsNoTracking().AsQueryable();

        if (customerId is not null)
        {
            var id = customerId.Value;
            meters = meters.Where(m => m.CustomerId == id);
        }

        if (meterId is not null)
        {
            var id = meterId.Value;
            meters = meters.Where(m => m.Id == id);
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
