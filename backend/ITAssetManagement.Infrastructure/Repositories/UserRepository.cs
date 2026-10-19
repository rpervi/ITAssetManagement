using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Domain.Entities;
using ITAssetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ITAssetManagement.Infrastructure.Repositories;

public sealed class UserRepository(AssetManagementDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken)
    {
        return GetUserQuery()
            .SingleOrDefaultAsync(user => user.NormalizedUsername == normalizedUsername, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return GetUserQuery()
            .SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        return GetUserQuery().SingleOrDefaultAsync(user => user.UserId == userId, cancellationToken);
    }

    public Task<UserSession?> GetSessionByIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        return dbContext.UserSessions
            .Include(session => session.User)!
                .ThenInclude(user => user!.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(session => session.SessionId == sessionId, cancellationToken);
    }

    public Task<UserSession?> GetSessionByRefreshTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return dbContext.UserSessions
            .Include(session => session.User)!
                .ThenInclude(user => user!.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(
                session => session.RefreshTokenHash == tokenHash,
                cancellationToken);
    }

    public Task<PasswordResetToken?> GetPasswordResetTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return dbContext.PasswordResetTokens
            .Include(token => token.User)!
                .ThenInclude(user => user!.UserRoles)
                    .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public Task<Role?> GetActiveRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken)
    {
        return dbContext.Roles.SingleOrDefaultAsync(
            role => role.RoleName == roleName && role.IsActive,
            cancellationToken);
    }

    public async Task<bool> ProvisionFirstAdminAsync(
        User user,
        Role adminRole,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var adminExists = await dbContext.UserRoles.AnyAsync(
            userRole => userRole.RoleId == adminRole.RoleId,
            cancellationToken);
        if (adminExists)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        user.UserRoles.Add(new UserRole
        {
            Role = adminRole,
            User = user,
            CreatedOn = DateTime.UtcNow,
        });
        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task IncrementFailedLoginCountAsync(
        int userId,
        int lockoutThreshold,
        DateTime now,
        DateTime lockoutEnd,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext.Users
            .Where(user => user.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        user => user.FailedLoginCount,
                        user => user.FailedLoginCount + 1 >= lockoutThreshold
                            ? 0
                            : user.FailedLoginCount + 1)
                    .SetProperty(
                        user => user.LockoutEndOn,
                        user => user.FailedLoginCount + 1 >= lockoutThreshold
                            ? lockoutEnd
                            : user.LockoutEndOn)
                    .SetProperty(user => user.ModifiedOn, now),
                cancellationToken);

        if (updated != 1)
        {
            throw new InvalidOperationException("The user account could not be updated.");
        }
    }

    public async Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public async Task AddSessionAsync(UserSession session, CancellationToken cancellationToken)
    {
        await dbContext.UserSessions.AddAsync(session, cancellationToken);
    }

    public async Task AddPasswordResetTokenAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken)
    {
        await dbContext.PasswordResetTokens.AddAsync(token, cancellationToken);
    }

    public Task RevokeSessionsAsync(
        int userId,
        DateTime revokedOn,
        CancellationToken cancellationToken)
    {
        return dbContext.UserSessions
            .Where(session => session.UserId == userId && session.RevokedOn == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(session => session.RevokedOn, revokedOn),
                cancellationToken);
    }

    public async Task RevokePasswordResetTokensAsync(
        int userId,
        DateTime usedOn,
        CancellationToken cancellationToken)
    {
        await dbContext.PasswordResetTokens
            .Where(token => token.UserId == userId && token.UsedOn == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.UsedOn, usedOn),
                cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(
        int userId,
        int tokenId,
        string passwordHash,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var consumed = await dbContext.PasswordResetTokens
            .Where(token => token.TokenId == tokenId
                && token.UserId == userId
                && token.UsedOn == null
                && token.ExpiresOn > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.UsedOn, now),
                cancellationToken);

        if (consumed != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var userUpdated = await dbContext.Users
            .Where(user => user.UserId == userId && user.IsActive)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.PasswordHash, passwordHash)
                    .SetProperty(user => user.FailedLoginCount, 0)
                    .SetProperty(user => user.LockoutEndOn, (DateTime?)null)
                    .SetProperty(user => user.ModifiedOn, now),
                cancellationToken);

        if (userUpdated != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await dbContext.UserSessions
            .Where(session => session.UserId == userId && session.RevokedOn == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(session => session.RevokedOn, now),
                cancellationToken);
        await dbContext.PasswordResetTokens
            .Where(token => token.UserId == userId && token.UsedOn == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.UsedOn, now),
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<User> GetUserQuery()
    {
        return dbContext.Users
            .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role);
    }
}
