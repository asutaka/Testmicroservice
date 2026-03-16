using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? connectionString = null)
    {
        if (string.IsNullOrEmpty(connectionString))
            services.AddDbContext<OrderDbContext>(o => o.UseInMemoryDatabase("OrderDb"));
        else
            services.AddDbContext<OrderDbContext>(o => o.UseNpgsql(connectionString,
                sql => sql.EnableRetryOnFailure(maxRetryCount: 5)));

        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }
}
