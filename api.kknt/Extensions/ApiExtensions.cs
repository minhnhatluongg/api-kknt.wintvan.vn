using api.kknt.API.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

namespace api.kknt.API.Extensions;

/// <summary>
/// Đăng ký toàn bộ service của tầng API: Controllers, Swagger, Versioning,
/// CORS, HealthChecks, ForwardedHeaders.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddControllersAndExplorer()
            .AddApiVersioningConfigured()
            .AddSwaggerConfigured()
            .AddCorsConfigured(configuration)
            .AddHealthChecksConfigured(configuration)
            .AddForwardedHeadersConfigured();

        return services;
    }

    // ── Controllers + Endpoint Explorer ──────────────────────────────────
    private static IServiceCollection AddControllersAndExplorer(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        return services;
    }

    // ── ForwardedHeaders (bắt buộc khi chạy sau IIS / reverse proxy) ─────
    private static IServiceCollection AddForwardedHeadersConfigured(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(opt =>
        {
            opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                 | ForwardedHeaders.XForwardedProto
                                 | ForwardedHeaders.XForwardedHost;
            opt.KnownNetworks.Clear();
            opt.KnownProxies.Clear();
        });
        return services;
    }
}

/// <summary>
/// Cấu hình toàn bộ middleware pipeline của API.
/// Thứ tự middleware rất quan trọng — không được thay đổi nếu không hiểu rõ.
/// </summary>
public static class WebApplicationExtensions
{
    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        // 1. Forwarded headers PHẢI chạy trước mọi middleware khác
        app.UseForwardedHeaders();

        // 2. Global exception – bắt mọi lỗi phía dưới
        app.UseMiddleware<GlobalExceptionMiddleware>();

        // 3. Serilog request logging – log method/path/status/latency cho mỗi request
        app.UseSerilogRequestLogging();

        // 4. Swagger (bật theo config, không phụ thuộc IsDevelopment)
        app.UseSwaggerConfigured();

        // 5. HTTPS redirect (IIS thường đã terminate SSL, nhưng vẫn giữ để chuẩn hóa)
        app.UseHttpsRedirection();

        // 6. Routing – cần trước CORS/Auth khi dùng endpoint-based middleware
        app.UseRouting();

        // 7. CORS – PHẢI đứng trước Authentication/Authorization
        app.UseCors(CorsExtensions.DefaultPolicy);

        // 8. Auth
        app.UseAuthentication();
        app.UseAuthorization();

        // 9. Endpoints
        app.MapControllers();
        app.MapHealthChecksConfigured();

        return app;
    }
}
