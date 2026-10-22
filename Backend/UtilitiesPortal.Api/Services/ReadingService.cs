using Microsoft.EntityFrameworkCore;
using UtilitiesPortal.Api.Data;
using UtilitiesPortal.Api.DTOs;
using UtilitiesPortal.Api.Errors;
using UtilitiesPortal.Api.Models;

namespace UtilitiesPortal.Api.Services;

public interface IReadingService
{
    /// <summary>The readings of one meter, newest first. Customers can only see their own meters.</summary>
    Task<List<ReadingDto>> GetReadingsAsync(Guid meterId, Guid? customerId, bool isAdmin);

    /// <summary>Records a new reading after checking the business rules.</summary>
    Task<ReadingDto> CreateAsync(CreateReadingRequest request);
}

public class ReadingService : IReadingService
{
    private readonly UtilitiesPortalContext _db;

    public ReadingService(UtilitiesPortalContext db)
    {
        _db = db;
    }

    public async Task<List<ReadingDto>> GetReadingsAsync(Guid meterId, Guid? customerId, bool isAdmin)
    {
        var meter = await _db.Meters.AsNoTracking().FirstOrDefaultAsync(m => m.Id == meterId);

        // A customer asking for someone else's meter gets the same answer as for a meter that
        // does not exist, so the API never confirms which meter ids are real.
        if (meter is null || (!isAdmin && meter.CustomerId != customerId))
        {
            throw new NotFoundException("Meter not found.");
        }

        var readings = await _db.MeterReadings
            .AsNoTracking()
            .Where(r => r.MeterId == meterId)
            .OrderBy(r => r.ReadingDate)
            .ToListAsync();

        // Oldest to newest, so each reading can subtract the one before it.
        var result = new List<ReadingDto>();
        decimal? previousKwh = null;
        foreach (var reading in readings)
        {
            result.Add(ToDto(reading, previousKwh));
            previousKwh = reading.KwhReading;
        }

        result.Reverse(); // newest first for the screen
        return result;
    }

    public async Task<ReadingDto> CreateAsync(CreateReadingRequest request)
    {
        // [Required] on the DTO already guarantees these three have values.
        var meterId = request.MeterId!.Value;
        var kwh = request.KwhReading!.Value;
        var readingDate = ToUtc(request.ReadingDate!.Value);

        if (decimal.Round(kwh, 2) != kwh)
        {
            throw new BadRequestException("The reading can have at most 2 decimal places.");
        }

        if (readingDate > DateTime.UtcNow)
        {
            throw new BadRequestException("The reading date cannot be in the future.");
        }

        var meter = await _db.Meters.AsNoTracking().FirstOrDefaultAsync(m => m.Id == meterId)
                    ?? throw new NotFoundException("Meter not found.");

        if (meter.Status != "Active")
        {
            throw new BadRequestException("Readings can only be added to an active meter.");
        }

        var duplicate = await _db.MeterReadings
            .AnyAsync(r => r.MeterId == meterId && r.ReadingDate == readingDate);
        if (duplicate)
        {
            throw new ConflictException("A reading already exists for this meter at that date and time.");
        }

        // The readings on either side of the new one, by date.
        var previous = await _db.MeterReadings
            .AsNoTracking()
            .Where(r => r.MeterId == meterId && r.ReadingDate < readingDate)
            .OrderByDescending(r => r.ReadingDate)
            .FirstOrDefaultAsync();

        var next = await _db.MeterReadings
            .AsNoTracking()
            .Where(r => r.MeterId == meterId && r.ReadingDate > readingDate)
            .OrderBy(r => r.ReadingDate)
            .FirstOrDefaultAsync();

        // A meter only counts up. The database trigger enforces the same rule as a safety net;
        // checking here lets us give a message with the actual numbers.
        if (previous is not null && kwh < previous.KwhReading)
        {
            throw new BadRequestException(
                $"The reading must be at least {previous.KwhReading:N2} kWh, the previous reading ({previous.ReadingDate:yyyy-MM-dd}).");
        }

        if (next is not null && kwh > next.KwhReading)
        {
            throw new BadRequestException(
                $"The reading must be at most {next.KwhReading:N2} kWh, the next reading ({next.ReadingDate:yyyy-MM-dd}).");
        }

        var reading = new MeterReading
        {
            MeterId = meterId,
            ReadingDate = readingDate,
            KwhReading = kwh
        };

        _db.MeterReadings.Add(reading);
        await _db.SaveChangesAsync();

        return ToDto(reading, previous?.KwhReading);
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static ReadingDto ToDto(MeterReading reading, decimal? previousKwh)
    {
        return new ReadingDto
        {
            Id = reading.Id,
            MeterId = reading.MeterId,
            ReadingDate = DateTime.SpecifyKind(reading.ReadingDate, DateTimeKind.Utc),
            KwhReading = reading.KwhReading,
            KwhUsed = previousKwh is null ? null : reading.KwhReading - previousKwh
        };
    }
}
