using ITAssetManagement.Application.Security;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ITAssetManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthServiceImpl>();
        services.AddScoped<IAssetCategoryService, AssetCategoryServiceImpl>();
        services.AddScoped<IAssetProductService, AssetProductServiceImpl>();
        services.AddScoped<IEmployeeService, EmployeeServiceImpl>();
        services.AddScoped<IStockService, StockServiceImpl>();
        services.AddScoped<IAssetAssignmentService, AssetAssignmentServiceImpl>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.Configure<PasswordOptions>(options =>
        {
            options.RequiredLength = 12;
            options.RequireDigit = true;
            options.RequireLowercase = true;
            options.RequireUppercase = true;
            options.RequireNonAlphanumeric = true;
        });

        return services;
    }
}
