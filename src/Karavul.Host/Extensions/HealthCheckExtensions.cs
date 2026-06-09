using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Karavul.Data.Database;
using Dapper;

namespace Karavul.Host.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddKaravulHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("Karavul Internal Services", () => HealthCheckResult.Healthy(), tags: ["core", "system"])
            .AddCheck<SqliteHealthCheck>("Karavul Database (SQLite)", tags: ["db", "sqlite"]);

        return services;
    }

    public static IEndpointRouteBuilder MapKaravulHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/Health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    totalDuration = report.TotalDuration.ToString(),
                    entries = report.Entries.ToDictionary(
                        e => e.Key,
                        e => new
                        {
                            data = e.Value.Data,
                            duration = e.Value.Duration.ToString(),
                            status = e.Value.Status.ToString(),
                            tags = e.Value.Tags
                        })
                };
                await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, response);
            }
        });

        return endpoints;
    }
}

public class SqliteHealthCheck : IHealthCheck
{
    private readonly DbConnectionFactory _factory;

    public SqliteHealthCheck(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var conn = _factory.CreateConnection();
            await SqlMapper.ExecuteScalarAsync<int>(conn, "SELECT 1");
            return HealthCheckResult.Healthy("Database connection is healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed.", ex);
        }
    }
}
