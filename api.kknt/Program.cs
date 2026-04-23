using api.kknt.API.Extensions;
using api.kknt.Application;
using api.kknt.Application.ImplementService;
using api.kknt.Application.InterfaceServices;
using api.kknt.Application.Options;
using api.kknt.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

// ── 1. Bootstrap logger ──────────────────────────────────────────────────
// Log sớm từ trước khi host được build, đề phòng startup lỗi.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting api.kknt host...");

    var builder = WebApplication.CreateBuilder(args);

    // ── 2. Serilog đọc config từ appsettings.json -> section "Serilog" ───
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "api.kknt"));

    // ── 3. Cấu hình IIS In-Process (khi host IIS sẽ được áp dụng) ───────
    builder.WebHost.UseIISIntegration();

    // ── 4. DI – tầng dưới trước, API sau cùng ───────────────────────────
    builder.Services
        .AddInfrastructure(builder.Configuration)
        .AddApplication(builder.Configuration)
        .AddApiServices(builder.Configuration);

    // ── 5. Authentication / JWT ─────────────────────────────────────────
    var jwtSettings = builder.Configuration
        .GetSection("JwtSettings").Get<JwtSettings>()
        ?? throw new InvalidOperationException(
            "Section 'JwtSettings' chưa được cấu hình trong appsettings.json");

    builder.Services.AddHttpClient<ITctLoginClient, TctLoginClient>((sp, client) =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var baseUrl = cfg["TctApi:BaseUrl"] ?? throw new InvalidOperationException("Missing TctApi:BaseUrl");
        var key = cfg["TctApi:InternalKey"] ?? throw new InvalidOperationException("Missing TctApi:InternalKey");
        var timeout = int.TryParse(cfg["TctApi:TimeoutSeconds"], out var t) ? t : 30;

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(timeout);
        client.DefaultRequestHeaders.Add("X-internal-key", key);
        client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    });

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidateIssuer           = true,
                ValidIssuer              = jwtSettings.Issuer,
                ValidateAudience         = true,
                ValidAudience            = jwtSettings.Audience,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    // ── 6. Build & run ──────────────────────────────────────────────────
    var app = builder.Build();

    app.UseInfrastructure();

    Log.Information("api.kknt host started. Env={Env}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "api.kknt host terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Dùng cho integration test (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program { }
