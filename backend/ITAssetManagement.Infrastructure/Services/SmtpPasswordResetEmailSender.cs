using ITAssetManagement.Application.Security;
using ITAssetManagement.Application.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ITAssetManagement.Infrastructure.Services;

public sealed class SmtpPasswordResetEmailSender(IOptions<SmtpSettings> settings)
    : IPasswordResetEmailSender
{
    private readonly SmtpSettings _settings = settings.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Host)
        && !string.IsNullOrWhiteSpace(_settings.SenderEmail)
        && _settings.Port is > 0 and <= 65535;

    public async Task SendResetLinkAsync(
        string email,
        string resetLink,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("SMTP password reset delivery is not configured.");
        }

        if (!Uri.TryCreate(_settings.FrontendBaseUrl, UriKind.Absolute, out var frontendUri)
            || (frontendUri.Scheme != Uri.UriSchemeHttps
                && !frontendUri.IsLoopback))
        {
            throw new InvalidOperationException(
                "The password reset frontend URL must use HTTPS outside local development.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Reset your IT Asset Management password";
        message.Body = new TextPart("plain")
        {
            Text = $"Use this one-time link to reset your password. It expires shortly: {resetLink}",
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(
            _settings.Host,
            _settings.Port,
            SecureSocketOptions.StartTls,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            if (string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException(
                    "Configure the SMTP password through a secret provider.");
            }

            await client.AuthenticateAsync(
                _settings.Username,
                _settings.Password,
                cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
