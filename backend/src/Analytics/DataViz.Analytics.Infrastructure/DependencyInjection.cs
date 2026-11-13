using Dapper;
using DataViz.Analytics.Application.Dashboard;
using DataViz.Analytics.Application.Projections;
using DataViz.Analytics.Infrastructure.ReadModel;
using DataViz.BuildingBlocks.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace DataViz.Analytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.Configure<AnalyticsOptions>(configuration.GetSection(AnalyticsOptions.SectionName));
        services.AddSingleton(_ => NpgsqlDataSource.Create(configuration.GetConnectionString("Database")!));
        services.AddScoped<ISalesProjection, SalesProjection>();
        services.AddScoped<IDashboardReadModel, DashboardReadModel>();

        return services.AddMessaging(configuration, Application.DependencyInjection.Assembly);
    }
}
