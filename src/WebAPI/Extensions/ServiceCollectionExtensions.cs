using Application.Abstractions;
using Application.Auth.Commands.Login;
using Application.Auth.Commands.Logout;
using Application.Auth.Commands.Refresh;
using Application.Auth.Commands.Register;
using Application.Common;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WebAPI.Options;

namespace WebAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddAuthServices(this IServiceCollection services)
    {
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<RefreshCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();

        services.AddScoped<PasswordFactory>();
    }

    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IDbContext, AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Postgres")));
        services.AddScoped<IAccessTokenBlacklist, RedisAccessTokenBlacklist>();
        services.AddSingleton<ConnectionMultiplexer>(ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IAuthorizationTokenGenerator, JwtGenerator>();
        services.AddScoped<IRefreshTokenHasher, RefreshTokenHasher>();

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        
        services.AddOptions<JwtConfig>().BindConfiguration(JwtConfig.Path);
        services.AddScoped<IJwtConfig>(services => services.GetRequiredService<IOptions<JwtConfig>>().Value);

        services.AddOptions<RefreshTokenConfig>().BindConfiguration(RefreshTokenConfig.Path);
        services.AddScoped<IRefreshTokenConfig>(services => services.GetRequiredService<IOptions<RefreshTokenConfig>>().Value);
    }
}