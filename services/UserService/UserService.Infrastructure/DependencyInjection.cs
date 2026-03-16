using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Interfaces;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Repositories;

namespace UserService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? connectionString = null)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            // Use InMemory for development/demo
            services.AddDbContext<UserDbContext>(options =>
                options.UseInMemoryDatabase("UserDb"));
        }
        else
        {
            services.AddDbContext<UserDbContext>(options =>
                options.UseNpgsql(connectionString, sql =>
                    sql.EnableRetryOnFailure(maxRetryCount: 5)));
        }

        services.AddScoped<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString ?? ""));
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
