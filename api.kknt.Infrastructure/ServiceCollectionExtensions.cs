using api.kknt.Application.InterfaceServices;
using api.kknt.Domain.Interfaces;
using api.kknt.Domain.Interfaces.DatabaseConfig;
using api.kknt.Infrastructure.AesEncryptionService;
using api.kknt.Infrastructure.Database;
using api.kknt.Infrastructure.Options;
using api.kknt.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace api.kknt.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- Options / Configuration ---
        services.Configure<MasterDbOptions>(configuration.GetSection(MasterDbOptions.Section));
        services.Configure<DemoDbOptions>(configuration.GetSection(DemoDbOptions.Section));
        services.Configure<ErpDbOptions>(configuration.GetSection(ErpDbOptions.Section));

        // --- Encryption (Stateless ➜ Singleton) ---
        services.AddSingleton<IEncryptionService, ShaImplement>();
        services.AddSingleton<IPasswordHasher,    Sha1PasswordHasher>();

        // --- Server-to-DB Resolver (giữ in-process cache ➜ Singleton) ---
        services.AddSingleton<IServerResolver, ServerResolver>();

        // --- DB Connection Factory (per-request ➜ Scoped) ---
        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<IRefreshTokenStore, DbRefreshTokenStore>();

        // --- Registration Repository (Dapper) ---
        services.AddScoped<IUserRegistrationRepository, UserRegistrationRepository>();
        services.AddScoped<ILoginCacheRepository, LoginCacheRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        return services;
    }
}
