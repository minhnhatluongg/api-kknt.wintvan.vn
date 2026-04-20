using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace api.kknt.API.Extensions;

/// <summary>
/// Health Checks:
///  - /health/live   : self check (app còn sống).
///  - /health/ready  : đầy đủ (DB, dependencies) – dùng cho K8s readiness / LB probe.
/// </summary>
public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthChecksConfigured(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var masterConn = configuration.GetValue<string>("MasterDb:ConnectionString");

        var builder = services.AddHealthChecks();

        if (!string.IsNullOrWhiteSpace(masterConn))
        {
            builder.AddSqlServer(
                connectionString: masterConn!,
                name: "master-db",
                tags: new[] { "ready", "db" });
        }

        return services;
    }

    public static IEndpointRouteBuilder MapHealthChecksConfigured(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate      = _ => false, 
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate      = r => r.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).AllowAnonymous();

        return endpoints;
    }
}
