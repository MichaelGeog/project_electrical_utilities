using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UtilitiesPortal.Api.Data;
using UtilitiesPortal.Api.DTOs;
using UtilitiesPortal.Api.Errors;
using UtilitiesPortal.Api.Models;

namespace UtilitiesPortal.Api.Services;

public interface IBillingService
{
    /// <summary>Creates a bill for a meter and period from the readings on the two boundary dates.</summary>
    Task<BillDto> GenerateAsync(GenerateBillRequest request);

    /// <summary>All bills of one customer, newest period first.</summary>
    Task<List<BillDto>> GetMyBillsAsync(Guid customerId);

    /// <summary>All bills (admin), optionally only Paid or only Unpaid.</summary>
    Task<List<BillDto>> GetAllBillsAsync(string? status);

    /// <summary>
    /// Marks a bill as paid (simulated: no real payment happens). A customer can only pay
    /// their own bills; an admin can mark any bill.
    /// </summary>
    Task<BillDto> PayAsync(Guid billId, Guid? customerId, bool isAdmin);
}

public class BillingService : IBillingService
{
    private readonly UtilitiesPortalContext _db;
    private readonly BillingOptions _options;

    public BillingService(UtilitiesPortalContext db, IOptions<BillingOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<BillDto> GenerateAsync(GenerateBillRequest request)
    {
        // [Required] on the DTO already guarantees these three have values.
        var meterId = request.MeterId!.Value;
        var periodStart = request.PeriodStart!.Value;
        var periodEnd = request.PeriodEnd!.Value;

        if (periodEnd <= periodStart)
        {
            throw new BadRequestException("The period end must be after the period start.");
        }

        if (periodEnd > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new BadRequestException("The billing period cannot end in the future.");
        }

        var meterExists = await _db.Meters.AnyAsync(m => m.Id == meterId);
        if (!meterExists)
        {
            throw new NotFoundException("Meter not found.");
        }

        // No overlapping bills: two periods overlap when each starts before the other ends.
        // This also catches an exact duplicate.
        var overlaps = await _db.Bills.AnyAsync(b =>
            b.MeterId == meterId && b.PeriodStart < periodEnd && b.PeriodEnd > periodStart);
        if (overlaps)
        {
            throw new ConflictException("This meter already has a bill that overlaps those dates.");
        }

        // Usage = the reading on the end date minus the reading on the start date.
        var startReading = await FindReadingOnAsync(meterId, periodStart);
        if (startReading is null)
        {
            throw new BadRequestException(
                $"There is no reading for this meter on {periodStart:yyyy-MM-dd}, so the usage can't be calculated.");
        }

        var endReading = await FindReadingOnAsync(meterId, periodEnd);
        if (endReading is null)
        {
            throw new BadRequestException(
                $"There is no reading for this meter on {periodEnd:yyyy-MM-dd}, so the usage can't be calculated.");
        }

        var kwhUsed = endReading.KwhReading - startReading.KwhReading;
        if (kwhUsed < 0)
        {
            throw new BadRequestException("The end reading is lower than the start reading. Check this meter's readings.");
        }

        // The price is worked out here, never taken from the client. Round half away from zero,
        // the same way SQL Server rounds (C#'s default would round halves to the even digit).
        var rate = _options.RatePerKwh;
        var amount = Math.Round(kwhUsed * rate, 2, MidpointRounding.AwayFromZero);

        var bill = new Bill
        {
            MeterId = meterId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            KwhUsed = kwhUsed,
            RatePerKwh = rate,
            Amount = amount,
            Status = "Unpaid",
            DueDate = periodEnd.AddDays(_options.DueDays)
        };

        _db.Bills.Add(bill);
        await _db.SaveChangesAsync();

        // Read the saved bill back through the summary view to get the customer and meter details.
        var summary = await _db.CustomerBillSummaries
            .AsNoTracking()
            .FirstAsync(b => b.BillId == bill.Id);

        return ToDto(summary);
    }

    public async Task<List<BillDto>> GetMyBillsAsync(Guid customerId)
    {
        var summaries = await _db.CustomerBillSummaries
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.PeriodEnd)
            .ThenBy(b => b.MeterNumber)
            .ToListAsync();

        return summaries.Select(ToDto).ToList();
    }

    public async Task<List<BillDto>> GetAllBillsAsync(string? status)
    {
        string? wanted = status?.Trim() switch
        {
            null or "" => null,
            var s when s.Equals("Paid", StringComparison.OrdinalIgnoreCase) => "Paid",
            var s when s.Equals("Unpaid", StringComparison.OrdinalIgnoreCase) => "Unpaid",
            _ => throw new BadRequestException("Status must be Paid or Unpaid.")
        };

        var query = _db.CustomerBillSummaries.AsNoTracking().AsQueryable();
        if (wanted is not null)
        {
            query = query.Where(b => b.Status == wanted);
        }

        var summaries = await query
            .OrderByDescending(b => b.PeriodEnd)
            .ThenBy(b => b.AccountNumber)
            .ThenBy(b => b.MeterNumber)
            .ToListAsync();

        return summaries.Select(ToDto).ToList();
    }

    public async Task<BillDto> PayAsync(Guid billId, Guid? customerId, bool isAdmin)
    {
        var summary = await _db.CustomerBillSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BillId == billId);

        // Someone else's bill gets the same 404 as a bill that doesn't exist.
        if (summary is null || (!isAdmin && summary.CustomerId != customerId))
        {
            throw new NotFoundException("Bill not found.");
        }

        if (summary.Status == "Paid")
        {
            throw new ConflictException("This bill is already paid.");
        }

        var bill = await _db.Bills.FirstAsync(b => b.Id == billId);
        bill.Status = "Paid";
        bill.PaidDate = DateOnly.FromDateTime(DateTime.UtcNow); // the database requires Paid bills to have a date
        await _db.SaveChangesAsync();

        var updated = await _db.CustomerBillSummaries
            .AsNoTracking()
            .FirstAsync(b => b.BillId == billId);

        return ToDto(updated);
    }

    /// <summary>The latest reading of a meter taken on a given calendar day (UTC), or null.</summary>
    private Task<MeterReading?> FindReadingOnAsync(Guid meterId, DateOnly day)
    {
        var from = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = from.AddDays(1);

        return _db.MeterReadings
            .AsNoTracking()
            .Where(r => r.MeterId == meterId && r.ReadingDate >= from && r.ReadingDate < to)
            .OrderByDescending(r => r.ReadingDate)
            .FirstOrDefaultAsync();
    }

    private static BillDto ToDto(CustomerBillSummary s)
    {
        return new BillDto
        {
            BillId = s.BillId,
            CustomerId = s.CustomerId,
            AccountNumber = s.AccountNumber,
            CustomerName = s.FullName,
            MeterId = s.MeterId,
            MeterNumber = s.MeterNumber,
            PeriodStart = s.PeriodStart,
            PeriodEnd = s.PeriodEnd,
            KwhUsed = s.KwhUsed,
            RatePerKwh = s.RatePerKwh,
            Amount = s.Amount,
            Status = s.Status,
            DueDate = s.DueDate,
            PaidDate = s.PaidDate
        };
    }
}
