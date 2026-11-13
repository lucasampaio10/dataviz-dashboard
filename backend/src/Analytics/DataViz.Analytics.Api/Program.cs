using DataViz.Analytics.Api;
using DataViz.Analytics.Application;
using DataViz.Analytics.Infrastructure;
using DataViz.Analytics.Infrastructure.ReadModel;
using DataViz.BuildingBlocks.Infrastructure.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddWebDefaults()
    .AddAnalyticsApplication()
    .AddAnalyticsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseWebDefaults();
app.MapDashboardEndpoints();

await app.CreateReadModelAsync();
await app.RunAsync();
