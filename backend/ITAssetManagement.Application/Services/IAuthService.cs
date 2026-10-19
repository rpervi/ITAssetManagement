using ITAssetManagement.Application.DTOs;

namespace ITAssetManagement.Application.Services;

public interface IAuthService
{
    Task<AuthSessionDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
    Task<AuthSessionDto?> RefreshSessionAsync(string refreshToken, CancellationToken cancellationToken);
    Task<AuthenticatedUserDto?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken);
    Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken);
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task<bool> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken);
    Task<bool> ProvisionFirstAdminAsync(
        string username,
        string email,
        string password,
        CancellationToken cancellationToken);
}
