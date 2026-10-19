namespace ITAssetManagement.Application.Security;

public sealed class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "IT Asset Management";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";
}
