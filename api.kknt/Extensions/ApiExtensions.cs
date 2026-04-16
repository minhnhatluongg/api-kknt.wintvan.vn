using api.kknt.API.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace api.kknt.API.Extensions;

/// <summary>
/// Gom toàn bộ đăng ký DI của tầng API (Controllers, Swagger) vào một chỗ.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }
}

/// <summary>
/// Gom toàn bộ cấu hình Middleware Pipeline vào một chỗ.
/// </summary>
public static class WebApplicationExtensions
{
    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
