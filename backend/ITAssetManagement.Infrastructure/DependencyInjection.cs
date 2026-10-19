using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Infrastructure.Persistence;
using ITAssetManagement.Infrastructure.Repositories;
using ITAssetManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ITAssetManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AssetManagementDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Configure the 'DefaultConnection' connection string before using the database.");
            }

            options.UseSqlServer(connectionString);
        });
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IAssetCategoryRepository, AssetCategoryRepository>();
        services.AddScoped<IAssetProductRepository, AssetProductRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IAssetAssignmentRepository, AssetAssignmentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordResetEmailSender, SmtpPasswordResetEmailSender>();

        return services;
    }
}
