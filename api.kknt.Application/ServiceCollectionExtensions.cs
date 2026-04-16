using api.kknt.Application.ImplementService;
using api.kknt.Application.InterfaceServices;
using api.kknt.Application.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace api.kknt.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- WinInvoice Options ---
        services.Configure<WinInvoiceOptions>(configuration.GetSection(WinInvoiceOptions.Section));
        // --- Add Services ---
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddScoped<IAuthService, AuthService>();

        services.AddHttpClient<IWinInvoiceService, WinInvoiceService>((serviceProvider, client) =>
        {
            var opts = serviceProvider.GetRequiredService<IOptions<WinInvoiceOptions>>().Value;

            if (string.IsNullOrWhiteSpace(opts.BaseUrl))
                throw new InvalidOperationException(
                    "WinInvoiceApi:BaseUrl chưa được cấu hình trong appsettings.json");

            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);

            var credentials = Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($"{opts.Username}:{opts.Password}"));

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        });
        return services;
    }
}
