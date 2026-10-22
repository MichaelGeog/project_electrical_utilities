namespace UtilitiesPortal.Api.Services;

/// <summary>
/// Billing settings, bound from the "Billing" section of appsettings.json.
/// A flat rate for now; tiered rates could be added here later.
/// </summary>
public class BillingOptions
{
    /// <summary>Price of one kWh. The database stores up to 4 decimal places.</summary>
    public decimal RatePerKwh { get; set; } = 0.25m;

    /// <summary>How many days after the period end the bill is due.</summary>
    public int DueDays { get; set; } = 21;
}
