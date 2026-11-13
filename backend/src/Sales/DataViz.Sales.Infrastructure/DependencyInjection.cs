using DataViz.BuildingBlocks.Infrastructure;
using DataViz.Sales.Application.Stores;
using DataViz.Sales.Domain.Sales;
using DataViz.Sales.Domain.Stores;
using DataViz.Sales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace DataViz.Sales.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSalesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<SalesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Database")));

        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ISalesQueries, SalesQueries>();
        services.TryAddSingleton(TimeProvider.System);

        return services
            .AddTransactionalOutbox<SalesDbContext>()
            .AddMessaging(configuration, Application.DependencyInjection.Assembly);
    }

    /// <summary>Opens the demo stores on a fresh database so sales have somewhere to happen.</summary>
    public static async Task SeedStoresAsync(this IHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        if (await context.Stores.AnyAsync())
        {
            return;
        }

        context.Stores.AddRange(
            Store.Open("Loja Aldeota", "Fortaleza"),
            Store.Open("Loja Boa Viagem", "Recife"),
            Store.Open("Loja Pinheiros", "São Paulo"),
            Store.Open("Loja Savassi", "Belo Horizonte"),
            Store.Open("Loja Moinhos", "Porto Alegre"));

        await context.SaveChangesAsync();
    }
}
