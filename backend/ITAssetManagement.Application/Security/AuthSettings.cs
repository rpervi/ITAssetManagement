namespace ITAssetManagement.Application.Security;

public sealed class AuthSettings
{
    public int LockoutThreshold { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int CookieMinutes { get; set; } = 15;
    public int SessionHours { get; set; } = 12;
    public int PasswordResetMinutes { get; set; } = 30;
}
