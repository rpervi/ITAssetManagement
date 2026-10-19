using ITAssetManagement.Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace ITAssetManagement.Api.Security;

public static class AuthAuthorizationPolicies
{
    public static void Configure(AuthorizationOptions options)
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.AddPolicy(AuthPolicies.ManageUsers, policy => policy.RequireRole("Admin"));
        options.AddPolicy(AuthPolicies.ManageAssets, policy => policy.RequireRole("Admin"));
        options.AddPolicy(AuthPolicies.ManageStock, policy => policy.RequireRole("Admin", "ITStaff"));
        options.AddPolicy(
            AuthPolicies.AssignAndReturnAssets,
            policy => policy.RequireRole("Admin", "ITStaff"));
        options.AddPolicy(
            AuthPolicies.ViewInventory,
            policy => policy.RequireRole("Admin", "ITStaff", "Viewer"));
        options.AddPolicy(
            AuthPolicies.ViewReports,
            policy => policy.RequireRole("Admin", "ITStaff", "Viewer"));
        options.AddPolicy(
            AuthPolicies.ViewEmployees,
            policy => policy.RequireRole("Admin", "ITStaff"));
    }
}
