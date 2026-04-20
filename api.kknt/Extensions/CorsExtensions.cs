namespace api.kknt.API.Extensions;

/// <summary>
/// CORS policy đọc từ section "Cors:AllowedOrigins" trong appsettings.
/// Production KHÔNG nên set "*"; cấu hình tường minh từng domain.
/// </summary>
public static class CorsExtensions
{
    public const string DefaultPolicy = "DefaultCorsPolicy";

    public static IServiceCollection AddCorsConfigured(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        var allowCredentials = configuration.GetValue("Cors:AllowCredentials", defaultValue: true);

        services.AddCors(opt =>
        {
            opt.AddPolicy(DefaultPolicy, policy =>
            {
                if (origins.Length == 0 || (origins.Length == 1 && origins[0] == "*"))
                {
                    // Dev/fallback: cho tất cả — KHÔNG được dùng chung AllowCredentials
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowAnyMethod()
                          .AllowAnyHeader();

                    if (allowCredentials) policy.AllowCredentials();
                }
                else
                {
                    policy.WithOrigins(origins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .WithExposedHeaders("X-Pagination", "X-Api-Version");

                    if (allowCredentials) policy.AllowCredentials();
                }
            });
        });

        return services;
    }
}
