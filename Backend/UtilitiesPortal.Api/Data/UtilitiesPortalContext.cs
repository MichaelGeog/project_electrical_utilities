using Microsoft.EntityFrameworkCore;
using UtilitiesPortal.Api.Models;

namespace UtilitiesPortal.Api.Data;

/// <summary>
/// The backend only talks to the views in the "api" schema, never to the tables in "data".
/// The database is the source of truth, so migrations are not used.
/// </summary>
public class UtilitiesPortalContext : DbContext
{
    public UtilitiesPortalContext(DbContextOptions<UtilitiesPortalContext> options)
        : base(options)
    {
    }

    // Writable views
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Meter> Meters => Set<Meter>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<OutageStatus> OutageStatuses => Set<OutageStatus>();

    // Read-only views (joins)
    public DbSet<MeterLatestReading> MeterLatestReadings => Set<MeterLatestReading>();
    public DbSet<CustomerBillSummary> CustomerBillSummaries => Set<CustomerBillSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Writable views are mapped like tables so SaveChanges inserts and updates through them.
        // ExcludeFromMigrations: EF never tries to create them, the SQL scripts own them.

        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("vw_Users", "api", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.PasswordHash).HasMaxLength(200);
            e.Property(x => x.Role).HasMaxLength(20);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("vw_Customers", "api", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.AccountNumber).HasMaxLength(20);
            e.Property(x => x.FullName).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(10);
            e.Property(x => x.ServiceAddress).HasMaxLength(200);
            e.Property(x => x.Region).HasMaxLength(30);
        });

        modelBuilder.Entity<Meter>(e =>
        {
            e.ToTable("vw_Meters", "api", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.MeterNumber).HasMaxLength(20);
            e.Property(x => x.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<MeterReading>(e =>
        {
            e.ToTable("vw_MeterReadings", "api", t =>
            {
                t.ExcludeFromMigrations();
                // The table behind this view has a trigger. This tells EF not to use an
                // OUTPUT clause, which SQL Server rejects on tables with triggers.
                t.HasTrigger("trg_MeterReadings_NoDecrease");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.KwhReading).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Bill>(e =>
        {
            e.ToTable("vw_Bills", "api", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.KwhUsed).HasPrecision(12, 2);
            e.Property(x => x.RatePerKwh).HasPrecision(8, 4);
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.Property(x => x.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<ServiceRequest>(e =>
        {
            e.ToTable("vw_ServiceRequests", "api", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(20);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<OutageStatus>(e =>
        {
            e.ToTable("vw_OutageStatus", "api", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.Region).HasMaxLength(30);
        });

        // Read-only join views: no key, query only. SaveChanges can never write to these.

        modelBuilder.Entity<MeterLatestReading>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_MeterLatestReading", "api");
            e.Property(x => x.KwhReading).HasPrecision(12, 2);
        });

        modelBuilder.Entity<CustomerBillSummary>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_CustomerBillSummary", "api");
            e.Property(x => x.KwhUsed).HasPrecision(12, 2);
            e.Property(x => x.RatePerKwh).HasPrecision(8, 4);
            e.Property(x => x.Amount).HasPrecision(10, 2);
        });
    }
}
