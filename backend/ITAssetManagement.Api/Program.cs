using System.Threading.RateLimiting;
using ITAssetManagement.Api.Controllers;
using ITAssetManagement.Api.Security;
using ITAssetManagement.Application;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Application.Security;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Infrastructure;
using ITAssetManagement.Domain.Entities;
using ITAssetManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("Authentication"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "__Host-ITAM.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
});
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-ITAM.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Path = "/";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(
            builder.Configuration.GetValue("Authentication:CookieMinutes", 15));
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = async context =>
        {
            var sessionValue = context.Principal?.FindFirstValue(AuthClaims.SessionId);
            if (!Guid.TryParse(sessionValue, out var sessionId))
            {
                context.RejectPrincipal();
                return;
            }

            var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            var session = await userRepository.GetSessionByIdAsync(
                sessionId,
                context.HttpContext.RequestAborted);
            if (session is null
                || session.RevokedOn is not null
                || session.ExpiresOn <= DateTime.UtcNow
                || session.User is not { IsActive: true })
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization(AuthAuthorizationPolicies.Configure);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (args.Contains("--provision-first-admin", StringComparer.Ordinal))
{
    await ProvisionFirstAdminAsync(app.Services);
}
else
{
    await app.RunAsync();
}

static async Task ProvisionFirstAdminAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AssetManagementDbContext>();
    if (!await dbContext.Database.CanConnectAsync())
    {
        throw new InvalidOperationException(
            "Cannot connect to SQL Server. Check DefaultConnection before provisioning an administrator.");
    }

    if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
    {
        throw new InvalidOperationException(
            "Apply the approved database migrations before provisioning the first administrator.");
    }

    var username = Environment.GetEnvironmentVariable("ITAM_FIRST_ADMIN_USERNAME");
    var email = Environment.GetEnvironmentVariable("ITAM_FIRST_ADMIN_EMAIL");
    var password = Environment.GetEnvironmentVariable("ITAM_FIRST_ADMIN_PASSWORD");
    if (username is null && email is null && password is null)
    {
        Console.Write("First administrator username: ");
        username = Console.ReadLine() ?? string.Empty;
        Console.Write("First administrator email: ");
        email = Console.ReadLine() ?? string.Empty;
        password = ReadPassword("Password (not echoed): ");
        var confirmation = ReadPassword("Confirm password (not echoed): ");
        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The password confirmation does not match.");
        }
    }
    else if (string.IsNullOrWhiteSpace(username)
        || string.IsNullOrWhiteSpace(email)
        || string.IsNullOrWhiteSpace(password))
    {
        throw new InvalidOperationException(
            "Set ITAM_FIRST_ADMIN_USERNAME, ITAM_FIRST_ADMIN_EMAIL, and ITAM_FIRST_ADMIN_PASSWORD together.");
    }

    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    var created = await authService.ProvisionFirstAdminAsync(
        username,
        email,
        password,
        CancellationToken.None);
    Console.WriteLine(created
        ? "The first administrator account was created."
        : "An administrator account already exists; no account was created.");
}

static string ReadPassword(string prompt)
{
    Console.Write(prompt);
    var password = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return password.ToString();
        }

        if (key.Key == ConsoleKey.Backspace && password.Length > 0)
        {
            password.Length--;
            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            password.Append(key.KeyChar);
        }
    }
}

public partial class Program;
