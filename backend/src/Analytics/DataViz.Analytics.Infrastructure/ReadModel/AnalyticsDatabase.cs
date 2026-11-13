using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DataViz.Analytics.Infrastructure.ReadModel;

public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    /// <summary>Time zone that decides on which business day a sale falls.</summary>
    public string TimeZone { get; set; } = "America/Fortaleza";
}

public static class AnalyticsDatabase
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    /// <summary>Creates the read model tables, waiting for the database container to accept connections.</summary>
    public static async Task CreateReadModelAsync(this IHost host)
    {
        var dataSource = host.Services.GetRequiredService<NpgsqlDataSource>();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AnalyticsDatabase));
        var schema = await ReadEmbeddedSchema();

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync();
                await connection.ExecuteAsync(schema);
                return;
            }
            catch (Exception exception) when (attempt < MaxAttempts)
            {
                logger.LogWarning("Database is not ready yet ({Reason}); attempt {Attempt}.", exception.Message, attempt);
                await Task.Delay(RetryDelay);
            }
        }
    }

    private static async Task<string> ReadEmbeddedSchema()
    {
        var assembly = typeof(AnalyticsDatabase).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith("Schema.sql", StringComparison.Ordinal));
        await using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);

        return await reader.ReadToEndAsync();
    }
}
