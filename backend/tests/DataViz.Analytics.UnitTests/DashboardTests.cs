using DataViz.Analytics.Application.Dashboard;
using NSubstitute;

namespace DataViz.Analytics.UnitTests;

public class DashboardTests
{
    private static readonly DashboardPeriod November = new(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

    private readonly IDashboardReadModel _readModel = Substitute.For<IDashboardReadModel>();

    [Fact]
    public void The_previous_period_has_the_same_length_and_ends_the_day_before()
    {
        var previous = November.Previous();

        Assert.Equal(new DateOnly(2026, 10, 2), previous.From);
        Assert.Equal(new DateOnly(2026, 10, 31), previous.To);
        Assert.Equal(November.Days, previous.Days);
    }

    [Fact]
    public void Indicators_are_derived_from_the_stored_totals()
    {
        var totals = new PeriodTotals(GrossRevenue: 10_000m, RefundedRevenue: 500m, Orders: 40, Refunds: 2);

        Assert.Equal(9_500m, totals.NetRevenue);
        Assert.Equal(250m, totals.AverageTicket);
        Assert.Equal(5m, totals.RefundRate);
    }

    [Fact]
    public void A_period_without_sales_has_zeroed_indicators_instead_of_dividing_by_zero()
    {
        var totals = new PeriodTotals(0m, 0m, 0, 0);

        Assert.Equal(0m, totals.AverageTicket);
        Assert.Equal(0m, totals.RefundRate);
    }

    [Theory]
    [InlineData(120, 100, 20.0)]
    [InlineData(75, 100, -25.0)]
    [InlineData(100, 100, 0.0)]
    public void The_change_is_relative_to_the_previous_value(decimal current, decimal previous, double expected)
    {
        Assert.Equal((decimal)expected, Indicator.Compare(current, previous).ChangePercent);
    }

    [Fact]
    public void There_is_no_change_to_report_when_the_previous_value_is_zero()
    {
        Assert.Null(Indicator.Compare(120m, 0m).ChangePercent);
    }

    [Fact]
    public async Task The_summary_compares_the_period_with_the_one_before_it()
    {
        _readModel.GetTotals(November, Arg.Any<CancellationToken>())
            .Returns(new PeriodTotals(12_000m, 0m, 60, 3));
        _readModel.GetTotals(November.Previous(), Arg.Any<CancellationToken>())
            .Returns(new PeriodTotals(10_000m, 0m, 40, 4));

        var result = await new GetSummaryHandler(_readModel).Handle(new GetSummaryQuery(November), CancellationToken.None);

        var summary = result.Value;
        Assert.Equal(new Indicator(12_000m, 10_000m, 20.0m), summary.NetRevenue);
        Assert.Equal(new Indicator(60m, 40m, 50.0m), summary.Orders);
        Assert.Equal(new Indicator(200m, 250m, -20.0m), summary.AverageTicket);
        Assert.Equal(new Indicator(5m, 10m, -50.0m), summary.RefundRate);
        Assert.Equal(new DateOnly(2026, 10, 2), summary.PreviousFrom);
    }

    [Fact]
    public async Task A_breakdown_reports_the_share_of_each_slice()
    {
        _readModel.GetBreakdown(November, BreakdownDimension.Channel, Arg.Any<CancellationToken>())
            .Returns([new BreakdownSlice("Online", 750m, 30), new BreakdownSlice("InStore", 250m, 10)]);

        var result = await new GetBreakdownHandler(_readModel)
            .Handle(new GetBreakdownQuery(November, "channel"), CancellationToken.None);

        Assert.Equal([75.0m, 25.0m], result.Value.Select(slice => slice.SharePercent));
    }

    [Fact]
    public async Task A_breakdown_without_revenue_has_no_shares()
    {
        _readModel.GetBreakdown(November, BreakdownDimension.Category, Arg.Any<CancellationToken>())
            .Returns([new BreakdownSlice("Fashion", 0m, 1)]);

        var result = await new GetBreakdownHandler(_readModel)
            .Handle(new GetBreakdownQuery(November, "Category"), CancellationToken.None);

        Assert.Equal(0m, Assert.Single(result.Value).SharePercent);
    }

    [Fact]
    public void A_period_that_ends_before_it_starts_is_invalid()
    {
        var backwards = new DashboardPeriod(new DateOnly(2026, 11, 30), new DateOnly(2026, 11, 1));

        var validation = new GetSummaryValidator().Validate(new GetSummaryQuery(backwards));

        Assert.False(validation.IsValid);
    }

    [Fact]
    public void A_period_longer_than_the_limit_is_invalid()
    {
        var tooLong = new DashboardPeriod(new DateOnly(2025, 1, 1), new DateOnly(2026, 11, 30));

        var validation = new GetSummaryValidator().Validate(new GetSummaryQuery(tooLong));

        Assert.False(validation.IsValid);
    }

    [Fact]
    public void An_unknown_breakdown_dimension_is_invalid()
    {
        var validation = new GetBreakdownValidator().Validate(new GetBreakdownQuery(November, "weather"));

        Assert.False(validation.IsValid);
    }
}
