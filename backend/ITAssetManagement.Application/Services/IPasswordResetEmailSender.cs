namespace ITAssetManagement.Application.Services;

public interface IPasswordResetEmailSender
{
    bool IsConfigured { get; }
    Task SendResetLinkAsync(string email, string resetLink, CancellationToken cancellationToken);
}
