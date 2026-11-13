using System.Reflection;
using DataViz.BuildingBlocks;
using Microsoft.Extensions.DependencyInjection;

namespace DataViz.Sales.Application;

public static class DependencyInjection
{
    public static readonly Assembly Assembly = typeof(DependencyInjection).Assembly;

    public static IServiceCollection AddSalesApplication(this IServiceCollection services) =>
        services.AddApplicationCore(Assembly);
}
