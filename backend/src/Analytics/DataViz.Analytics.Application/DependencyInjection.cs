using System.Reflection;
using DataViz.BuildingBlocks;
using Microsoft.Extensions.DependencyInjection;

namespace DataViz.Analytics.Application;

public static class DependencyInjection
{
    public static readonly Assembly Assembly = typeof(DependencyInjection).Assembly;

    public static IServiceCollection AddAnalyticsApplication(this IServiceCollection services) =>
        services.AddApplicationCore(Assembly);
}
