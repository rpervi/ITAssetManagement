using System.Security.Cryptography;
using System.Text;
using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Application.Security;
using ITAssetManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITAssetManagement.Application.Services;

public sealed class AuthServiceImpl(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    IPasswordResetEmailSender passwordResetEmailSender,
    IOptions<AuthSettings> authSettings,
    IOptions<PasswordOptions> passwordOptions,
    IOptions<SmtpSettings> smtpSettings,
    ILogger<AuthServiceImpl> logger) : IAuthService
{
    private readonly AuthSettings _auth = authSettings.Value;
    private readonly PasswordOptions _password = passwordOptions.Value;
    private readonly SmtpSettings _smtp = smtpSettings.Value;

    public async Task<AuthSessionDto?> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByUsernameAsync(
            Normalize(request.Username),
            cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        if (user.LockoutEndOn is { } lockoutEnd && lockoutEnd > now)
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            await userRepository.IncrementFailedLoginCountAsync(
                user.UserId,
                _auth.LockoutThreshold,
                now,
                now.AddMinutes(_auth.LockoutMinutes),
                cancellationToken);
            return null;
        }

        user.FailedLoginCount = 0;
        user.LockoutEndOn = null;
        user.LastLoginOn = now;
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        }

        var session = CreateSession(user, now);
        await userRepository.AddSessionAsync(session.Session, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
        return session.Dto;
    }

    public async Task<AuthSessionDto?> RefreshSessionAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var session = await userRepository.GetSessionByRefreshTokenHashAsync(
            HashToken(refreshToken),
            cancellationToken);
        if (session is null
            || session.RevokedOn is not null
            || session.ExpiresOn <= now
            || session.User is not { IsActive: true })
        {
            return null;
        }

        var rotatedToken = CreateOpaqueToken();
        session.RefreshTokenHash = HashToken(rotatedToken);
        session.ExpiresOn = now.AddHours(_auth.SessionHours);
        await userRepository.SaveChangesAsync(cancellationToken);

        return new AuthSessionDto(
            CreateUserDto(session.User),
            session.SessionId,
            rotatedToken,
            session.ExpiresOn);
    }

    public async Task<AuthenticatedUserDto?> GetCurrentUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is { IsActive: true } ? CreateUserDto(user) : null;
    }

    public async Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await userRepository.GetSessionByIdAsync(sessionId, cancellationToken);
        if (session is null || session.RevokedOn is not null)
        {
            return;
        }

        session.RevokedOn = DateTime.UtcNow;
        await userRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken)
    {
        if (!passwordResetEmailSender.IsConfigured)
        {
            logger.LogWarning("Password reset requested while SMTP delivery is not configured.");
            return;
        }

        var user = await userRepository.GetByEmailAsync(Normalize(email), cancellationToken);
        if (user is not { IsActive: true })
        {
            return;
        }

        var now = DateTime.UtcNow;
        await userRepository.RevokePasswordResetTokensAsync(user.UserId, now, cancellationToken);

        var token = CreateOpaqueToken();
        var resetToken = new PasswordResetToken
        {
            UserId = user.UserId,
            TokenHash = HashToken(token),
            ExpiresOn = now.AddMinutes(_auth.PasswordResetMinutes),
            CreatedOn = now,
        };
        await userRepository.AddPasswordResetTokenAsync(resetToken, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        var resetLink =
            $"{_smtp.FrontendBaseUrl.TrimEnd('/')}/reset-password"
            + $"?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";

        try
        {
            await passwordResetEmailSender.SendResetLinkAsync(
                user.Email,
                resetLink,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await userRepository.RevokePasswordResetTokensAsync(
                user.UserId,
                DateTime.UtcNow,
                cancellationToken);
            logger.LogError(exception, "Password reset email delivery failed.");
        }
    }

    public async Task<bool> ResetPasswordAsync(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!IsPasswordValid(request.NewPassword))
        {
            return false;
        }

        var reset = await userRepository.GetPasswordResetTokenAsync(
            HashToken(request.Token),
            cancellationToken);
        var now = DateTime.UtcNow;
        if (reset is not { UsedOn: null }
            || reset.ExpiresOn <= now
            || reset.User is not { IsActive: true }
            || reset.User.NormalizedEmail != Normalize(request.Email))
        {
            return false;
        }

        var passwordHash = passwordHasher.HashPassword(reset.User, request.NewPassword);
        return await userRepository.ResetPasswordAsync(
            reset.UserId,
            reset.TokenId,
            passwordHash,
            now,
            cancellationToken);
    }

    public async Task<bool> ProvisionFirstAdminAsync(
        string username,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = Normalize(username);
        var normalizedEmail = Normalize(email);
        if (string.IsNullOrWhiteSpace(normalizedUsername)
            || string.IsNullOrWhiteSpace(normalizedEmail)
            || !IsPasswordValid(password))
        {
            throw new ArgumentException(
                "A username, valid email, and password meeting the configured policy are required.");
        }

        var adminRole = await userRepository.GetActiveRoleByNameAsync("Admin", cancellationToken)
            ?? throw new InvalidOperationException(
                "The Admin role is missing. Apply the approved authentication migration first.");

        var user = new User
        {
            Username = username.Trim(),
            NormalizedUsername = normalizedUsername,
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = string.Empty,
            IsActive = true,
            CreatedOn = DateTime.UtcNow,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        return await userRepository.ProvisionFirstAdminAsync(user, adminRole, cancellationToken);
    }

    private (AuthSessionDto Dto, UserSession Session) CreateSession(User user, DateTime now)
    {
        var refreshToken = CreateOpaqueToken();
        var session = new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            RefreshTokenHash = HashToken(refreshToken),
            ExpiresOn = now.AddHours(_auth.SessionHours),
            CreatedOn = now,
        };

        return (
            new AuthSessionDto(
                CreateUserDto(user),
                session.SessionId,
                refreshToken,
                session.ExpiresOn),
            session);
    }

    private static AuthenticatedUserDto CreateUserDto(User user)
    {
        var roles = user.UserRoles
            .Where(userRole => userRole.Role is { IsActive: true })
            .Select(userRole => userRole.Role!.RoleName)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new AuthenticatedUserDto(user.UserId, user.Username, user.Email, roles);
    }

    private bool IsPasswordValid(string value)
    {
        return value.Length >= _password.RequiredLength
            && (!_password.RequireDigit || value.Any(char.IsDigit))
            && (!_password.RequireLowercase || value.Any(char.IsLower))
            && (!_password.RequireUppercase || value.Any(char.IsUpper))
            && (!_password.RequireNonAlphanumeric || value.Any(character => !char.IsLetterOrDigit(character)));
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string CreateOpaqueToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
