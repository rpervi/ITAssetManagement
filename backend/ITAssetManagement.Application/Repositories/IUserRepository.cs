using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Application.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken);
    Task<UserSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<UserSession?> GetSessionByRefreshTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);
    Task<PasswordResetToken?> GetPasswordResetTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken);
    Task<Role?> GetActiveRoleByNameAsync(string roleName, CancellationToken cancellationToken);
    Task<bool> ProvisionFirstAdminAsync(
        User user,
        Role adminRole,
        CancellationToken cancellationToken);
    Task IncrementFailedLoginCountAsync(
        int userId,
        int lockoutThreshold,
        DateTime now,
        DateTime lockoutEnd,
        CancellationToken cancellationToken);
    Task AddUserAsync(User user, CancellationToken cancellationToken);
    Task AddSessionAsync(UserSession session, CancellationToken cancellationToken);
    Task AddPasswordResetTokenAsync(PasswordResetToken token, CancellationToken cancellationToken);
    Task RevokeSessionsAsync(int userId, DateTime revokedOn, CancellationToken cancellationToken);
    Task RevokePasswordResetTokensAsync(
        int userId,
        DateTime usedOn,
        CancellationToken cancellationToken);
    Task<bool> ResetPasswordAsync(
        int userId,
        int tokenId,
        string passwordHash,
        DateTime now,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
