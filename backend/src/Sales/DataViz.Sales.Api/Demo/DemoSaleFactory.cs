using DataViz.Sales.Application.Sales;
using DataViz.Sales.Application.Stores;
using DataViz.Sales.Domain.Sales;

namespace DataViz.Sales.Api.Demo;

/// <summary>
/// Invents plausible retail sales: some stores sell more, weekends are busier, electronics cost
/// more than groceries and the business grows slowly. Shaped data makes the charts worth reading.
/// </summary>
internal sealed class DemoSaleFactory(IReadOnlyList<StoreResponse> stores, Random random)
{
    private const int BaseSalesPerDay = 90;
    private const double DailyGrowth = 0.6;
    private const int OpeningHourUtc = 12;
    private const int OpenHours = 12;
    private const int MaxHoursUntilRefund = 96;

    private static readonly (ProductCategory Category, int Weight, double AverageTicket)[] Categories =
    [
        (ProductCategory.Electronics, 16, 890),
        (ProductCategory.Fashion, 24, 185),
        (ProductCategory.Home, 14, 310),
        (ProductCategory.Beauty, 17, 96),
        (ProductCategory.Sports, 12, 215),
        (ProductCategory.Grocery, 17, 68),
    ];

    private static readonly (SalesChannel Channel, int Weight)[] Channels =
    [
        (SalesChannel.InStore, 46),
        (SalesChannel.Online, 37),
        (SalesChannel.Marketplace, 17),
    ];

    private static readonly int[] StoreWeights = [30, 22, 26, 13, 9];

    public int SalesOn(DateOnly day, int dayIndex)
    {
        var weekdayFactor = day.DayOfWeek switch
        {
            DayOfWeek.Saturday => 1.35,
            DayOfWeek.Friday => 1.2,
            DayOfWeek.Sunday => 0.75,
            DayOfWeek.Monday => 0.9,
            _ => 1.0,
        };
        var noise = 0.85 + (random.NextDouble() * 0.3);

        return (int)Math.Round((BaseSalesPerDay + (dayIndex * DailyGrowth)) * weekdayFactor * noise);
    }

    /// <summary>
    /// Time between two live sales, paced so a full day sells about as much as the days in the
    /// history. A faster demo would make "today" dwarf every other day on the chart.
    /// </summary>
    public TimeSpan TypicalGapOn(DateOnly day, int dayIndex) =>
        TimeSpan.FromSeconds(OpenHours * 3600.0 / Math.Max(1, SalesOn(day, dayIndex)) * (0.5 + random.NextDouble()));

    public static bool IsOpenAt(DateTime momentUtc) => momentUtc.Hour >= OpeningHourUtc;

    public RecordSaleCommand SaleAt(DateTime occurredAtUtc)
    {
        var (category, _, averageTicket) = Pick(Categories, entry => entry.Weight);
        var (channel, _) = Pick(Channels, entry => entry.Weight);
        var store = stores[PickIndex(StoreWeights.Take(stores.Count).ToArray())];

        return new RecordSaleCommand(
            store.Id,
            category.ToString(),
            channel.ToString(),
            Amount(averageTicket),
            occurredAtUtc);
    }

    public DateTime MomentDuringBusinessHours(DateOnly day)
    {
        var secondsAfterOpening = random.Next(OpenHours * 3600);
        return day.ToDateTime(new TimeOnly(OpeningHourUtc, 0), DateTimeKind.Utc).AddSeconds(secondsAfterOpening);
    }

    /// <summary>Customers who regret a purchase usually come back within a few days.</summary>
    public DateTime RefundMomentFor(DateTime saleMomentUtc) =>
        saleMomentUtc.AddHours(1 + (random.NextDouble() * MaxHoursUntilRefund));

    public bool Chance(double probability) => random.NextDouble() < probability;

    private decimal Amount(double averageTicket)
    {
        // Log-normal spread: most tickets sit near the average, a few are much larger.
        var spread = Math.Exp((random.NextDouble() + random.NextDouble() + random.NextDouble() - 1.5) * 0.7);
        return Math.Max(5m, Math.Round((decimal)(averageTicket * spread), 2));
    }

    private T Pick<T>(T[] options, Func<T, int> weightOf) =>
        options[PickIndex(options.Select(weightOf).ToArray())];

    private int PickIndex(int[] weights)
    {
        var roll = random.Next(weights.Sum());
        for (var index = 0; index < weights.Length; index++)
        {
            roll -= weights[index];
            if (roll < 0)
            {
                return index;
            }
        }

        return weights.Length - 1;
    }
}
