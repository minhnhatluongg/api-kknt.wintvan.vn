using Asp.Versioning;

namespace api.kknt.API.Extensions;

/// <summary>
/// API Versioning – dùng URL segment (api/v1/...), hỗ trợ thêm header/querystring.
/// </summary>
public static class VersioningExtensions
{
    public static IServiceCollection AddApiVersioningConfigured(this IServiceCollection services)
    {
        services.AddApiVersioning(opt =>
            {
                opt.DefaultApiVersion                  = new ApiVersion(1, 0);
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.ReportApiVersions                  = true;
                opt.ApiVersionReader                   = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("X-Api-Version"),
                    new QueryStringApiVersionReader("api-version"));
            })
            .AddMvc()
            .AddApiExplorer(opt =>
            {
                // Format: v'major[.minor][-status]  →  v1, v1.1, v2-beta
                opt.GroupNameFormat           = "'v'VVV";
                opt.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
