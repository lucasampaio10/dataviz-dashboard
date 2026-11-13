using DataViz.BuildingBlocks.Infrastructure.Persistence;
using DataViz.BuildingBlocks.Infrastructure.Web;
using DataViz.Sales.Api;
using DataViz.Sales.Api.Demo;
using DataViz.Sales.Application;
using DataViz.Sales.Infrastructure;
using DataViz.Sales.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddWebDefaults()
    .AddSalesApplication()
    .AddSalesInfrastructure(builder.Configuration)
    .AddDemoSales(builder.Configuration);

var app = builder.Build();

app.UseWebDefaults();
app.MapSalesEndpoints();

await app.MigrateDatabaseAsync<SalesDbContext>();
await app.SeedStoresAsync();
await app.RunAsync();
