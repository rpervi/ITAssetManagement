using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ITAssetManagement.Api.Security;
using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Application.Security;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ITAssetManagement.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Login_RejectsWrongUsernameAndPassword()
    {
        var (service, repository) = CreateService();
        var user = CreateUser(repository);

        var unknown = await service.LoginAsync(
            new LoginRequestDto("nobody", "Wrong-password1!"),
            CancellationToken.None);
        var wrongPassword = await service.LoginAsync(
            new LoginRequestDto(user.Username, "Wrong-password1!"),
            CancellationToken.None);

        Assert.Null(unknown);
        Assert.Null(wrongPassword);
        Assert.Equal(1, user.FailedLoginCount);
    }

    [Fact]
    public async Task Login_RejectsInactiveUser()
    {
        var (service, repository) = CreateService();
        var user = CreateUser(repository);
        user.IsActive = false;

        var result = await service.LoginAsync(
            new LoginRequestDto(user.Username, "Secure-password1!"),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(repository.Sessions);
    }

    [Fact]
    public async Task Login_LocksAccountAfterConfiguredFailedAttempts()
    {
        var (service, repository) = CreateService();
        var user = CreateUser(repository);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var result = await service.LoginAsync(
                new LoginRequestDto(user.Username, "Wrong-password1!"),
                CancellationToken.None);
            Assert.Null(result);
        }

        var lockedOutLogin = await service.LoginAsync(
            new LoginRequestDto(user.Username, "Secure-password1!"),
            CancellationToken.None);

        Assert.Null(lockedOutLogin);
        Assert.NotNull(user.LockoutEndOn);
        Assert.Empty(repository.Sessions);
    }

    [Fact]
    public async Task Refresh_RotatesTokenAndLogoutInvalidatesSession()
    {
        var (service, repository) = CreateService();
        var user = CreateUser(repository);
        var login = await service.LoginAsync(
            new LoginRequestDto(user.Username, "Secure-password1!"),
            CancellationToken.None);

        Assert.NotNull(login);
        Assert.NotEqual(login.RefreshToken, repository.Sessions.Values.Single().RefreshTokenHash);

        var refreshed = await service.RefreshSessionAsync(login.RefreshToken, CancellationToken.None);

        Assert.NotNull(refreshed);
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
        Assert.Null(await service.RefreshSessionAsync(login.RefreshToken, CancellationToken.None));

        await service.LogoutAsync(refreshed.SessionId, CancellationToken.None);

        Assert.Null(await service.RefreshSessionAsync(refreshed.RefreshToken, CancellationToken.None));
        Assert.NotNull(repository.Sessions.Values.Single().RevokedOn);
    }

    [Fact]
    public async Task RolePolicies_RestrictManagementAndAllowViewerReadAccess()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddAuthorization(AuthAuthorizationPolicies.Configure)
            .BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var viewer = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Viewer")],
            "test",
            ClaimTypes.Name,
            ClaimTypes.Role));
        var itStaff = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "ITStaff")],
            "test",
            ClaimTypes.Name,
            ClaimTypes.Role));

        Assert.True((await authorization.AuthorizeAsync(viewer, null, AuthPolicies.ViewReports)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(viewer, null, AuthPolicies.ViewEmployees)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(viewer, null, AuthPolicies.ManageUsers)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(itStaff, null, AuthPolicies.ManageStock)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(itStaff, null, AuthPolicies.ViewEmployees)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(itStaff, null, AuthPolicies.ManageUsers)).Succeeded);
    }

    private static (AuthServiceImpl Service, FakeUserRepository Repository) CreateService()
    {
        var repository = new FakeUserRepository();
        var hasher = new PasswordHasher<User>();
        var authSettings = Options.Create(new AuthSettings
        {
            LockoutThreshold = 5,
            LockoutMinutes = 15,
            CookieMinutes = 15,
            SessionHours = 12,
            PasswordResetMinutes = 30,
        });
        var passwordOptions = Options.Create(new PasswordOptions
        {
            RequiredLength = 12,
            RequireDigit = true,
            RequireLowercase = true,
            RequireUppercase = true,
            RequireNonAlphanumeric = true,
        });

        var service = new AuthServiceImpl(
            repository,
            hasher,
            new FakePasswordResetEmailSender(),
            authSettings,
            passwordOptions,
            Options.Create(new SmtpSettings()),
            NullLogger<AuthServiceImpl>.Instance);

        return (service, repository);
    }

    private static User CreateUser(FakeUserRepository repository)
    {
        var user = new User
        {
            UserId = 1,
            Username = "alice",
            NormalizedUsername = "ALICE",
            Email = "alice@example.test",
            NormalizedEmail = "ALICE@EXAMPLE.TEST",
            PasswordHash = string.Empty,
            IsActive = true,
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "Secure-password1!");
        user.UserRoles.Add(new UserRole
        {
            UserId = user.UserId,
            Role = new Role { RoleId = 1, RoleName = "Admin", IsActive = true },
        });
        repository.Users.Add(user.UserId, user);
        return user;
    }

    private sealed class FakePasswordResetEmailSender : IPasswordResetEmailSender
    {
        public bool IsConfigured => false;

        public Task SendResetLinkAsync(string email, string resetLink, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The fake email sender is not configured.");
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Dictionary<int, User> Users { get; } = [];
        public Dictionary<Guid, UserSession> Sessions { get; } = [];

        public Task<User?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken)
        {
            return Task.FromResult(Users.Values.SingleOrDefault(user =>
                user.NormalizedUsername == normalizedUsername));
        }

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return Task.FromResult(Users.Values.SingleOrDefault(user =>
                user.NormalizedEmail == normalizedEmail));
        }

        public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken)
        {
            Users.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public Task<UserSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken)
        {
            Sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }

        public Task<UserSession?> GetSessionByRefreshTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Sessions.Values.SingleOrDefault(session =>
                session.RefreshTokenHash == tokenHash));
        }

        public Task<PasswordResetToken?> GetPasswordResetTokenAsync(
            string tokenHash,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<PasswordResetToken?>(null);
        }

        public Task<Role?> GetActiveRoleByNameAsync(string roleName, CancellationToken cancellationToken)
        {
            return Task.FromResult<Role?>(null);
        }

        public Task<bool> ProvisionFirstAdminAsync(
            User user,
            Role adminRole,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task IncrementFailedLoginCountAsync(
            int userId,
            int lockoutThreshold,
            DateTime now,
            DateTime lockoutEnd,
            CancellationToken cancellationToken)
        {
            var user = Users[userId];
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= lockoutThreshold)
            {
                user.FailedLoginCount = 0;
                user.LockoutEndOn = lockoutEnd;
            }

            return Task.CompletedTask;
        }

        public Task AddUserAsync(User user, CancellationToken cancellationToken)
        {
            Users.Add(user.UserId, user);
            return Task.CompletedTask;
        }

        public Task AddSessionAsync(UserSession session, CancellationToken cancellationToken)
        {
            Sessions.Add(session.SessionId, session);
            return Task.CompletedTask;
        }

        public Task AddPasswordResetTokenAsync(
            PasswordResetToken token,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task RevokeSessionsAsync(int userId, DateTime revokedOn, CancellationToken cancellationToken)
        {
            foreach (var session in Sessions.Values.Where(session => session.UserId == userId))
            {
                session.RevokedOn = revokedOn;
            }

            return Task.CompletedTask;
        }

        public Task RevokePasswordResetTokensAsync(
            int userId,
            DateTime usedOn,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ResetPasswordAsync(
            int userId,
            int tokenId,
            string passwordHash,
            DateTime now,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
