using FluentValidation;

namespace DataViz.Analytics.Application.Dashboard;

/// <summary>Inclusive range of days, optionally narrowed to one store.</summary>
public sealed record DashboardPeriod(DateOnly From, DateOnly To, Guid? StoreId = null)
{
    public const int MaxDays = 366;

    public int Days => To.DayNumber - From.DayNumber + 1;

    /// <summary>The range of the same length that ends the day before this one starts.</summary>
    public DashboardPeriod Previous() => this with { From = From.AddDays(-Days), To = From.AddDays(-1) };
}

public interface IPeriodQuery
{
    DashboardPeriod Period { get; }
}

internal abstract class PeriodQueryValidator<TQuery> : AbstractValidator<TQuery>
    where TQuery : IPeriodQuery
{
    protected PeriodQueryValidator()
    {
        RuleFor(query => query.Period.To)
            .GreaterThanOrEqualTo(query => query.Period.From)
            .WithName("to")
            .WithMessage("'to' must not be before 'from'.");

        RuleFor(query => query.Period.Days)
            .LessThanOrEqualTo(DashboardPeriod.MaxDays)
            .WithName("from")
            .WithMessage($"The period cannot be longer than {DashboardPeriod.MaxDays} days.");
    }
}
