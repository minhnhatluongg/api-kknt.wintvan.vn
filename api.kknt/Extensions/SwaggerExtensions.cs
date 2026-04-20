using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace api.kknt.API.Extensions;

/// <summary>
/// Cấu hình Swagger/OpenAPI: JWT Bearer, XML comments, multi-version grouping.
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerConfigured(this IServiceCollection services)
    {
        // ConfigureOptions chạy sau khi IApiVersionDescriptionProvider đã sẵn sàng
        services.ConfigureOptions<ConfigureSwaggerOptions>();

        services.AddSwaggerGen(opt =>
        {
            // JWT Bearer Auth trong Swagger UI
            var bearerScheme = new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "Nhập JWT token. Format: {your token} (không cần prefix 'Bearer ').",
                Reference    = new OpenApiReference
                {
                    Id   = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            };
            opt.AddSecurityDefinition("Bearer", bearerScheme);
            opt.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [bearerScheme] = Array.Empty<string>()
            });

            // Hỗ trợ [SwaggerOperation], [SwaggerResponse]…
            opt.EnableAnnotations();

            // Nạp XML documentation của tất cả assembly trong bin/
            IncludeXmlComments(opt);

            // Resolve đụng tên DTO cùng tên ở namespace khác nhau
            opt.CustomSchemaIds(t => t.FullName?.Replace("+", "."));
        });

        return services;
    }

    public static WebApplication UseSwaggerConfigured(this WebApplication app)
    {
        var swaggerSection = app.Configuration.GetSection("Swagger");
        var enabled = swaggerSection.GetValue("Enabled", defaultValue: true);
        if (!enabled) return app;

        var routePrefix = swaggerSection.GetValue("RoutePrefix", defaultValue: "swagger");

        app.UseSwagger(opt =>
        {
            // Nếu chạy IIS dưới virtual directory, header X-Forwarded-Prefix sẽ được honor
            opt.RouteTemplate = routePrefix + "/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(opt =>
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            foreach (var desc in provider.ApiVersionDescriptions)
            {
                // Dùng relative path để vẫn hoạt động khi deploy dưới virtual directory IIS
                opt.SwaggerEndpoint(
                    $"{desc.GroupName}/swagger.json",
                    $"api.kknt API {desc.GroupName.ToUpperInvariant()}");
            }
            opt.RoutePrefix            = routePrefix;
            opt.DocumentTitle          = "api.kknt – Swagger UI";
            opt.DefaultModelsExpandDepth(-1);   // ẩn "Schemas" ở cuối
            opt.DisplayRequestDuration();
        });

        // Root "/" ➜ redirect sang /swagger để truy cập domain gốc không bị 404
        app.MapGet("/", (HttpContext ctx) =>
        {
            var prefix = ctx.Request.PathBase.HasValue
                ? ctx.Request.PathBase.Value!.TrimEnd('/')
                : string.Empty;
            return Results.Redirect($"{prefix}/{routePrefix}", permanent: false);
        })
        .ExcludeFromDescription()   // không hiển thị trong Swagger
        .AllowAnonymous();

        return app;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static void IncludeXmlComments(SwaggerGenOptions opt)
    {
        var baseDir = AppContext.BaseDirectory;
        foreach (var xml in Directory.GetFiles(baseDir, "*.xml", SearchOption.TopDirectoryOnly))
        {
            try
            {
                opt.IncludeXmlComments(xml, includeControllerXmlComments: true);
            }
            catch
            {
                // Bỏ qua file xml không phải docs (ví dụ config xml)
            }
        }
    }
}

/// <summary>
/// Sinh 1 SwaggerDoc cho mỗi API version đã đăng ký.
/// </summary>
internal sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
        => _provider = provider;

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var desc in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(desc.GroupName, new OpenApiInfo
            {
                Title       = "api.kknt API",
                Version     = desc.ApiVersion.ToString(),
                Description = desc.IsDeprecated
                    ? "⚠ Phiên bản API này đã bị deprecated."
                    : "REST API cho hệ thống KKNT.",
                Contact = new OpenApiContact
                {
                    Name  = "api.kknt Team",
                    Email = "support@kknt.vn"
                }
            });
        }
    }
}
