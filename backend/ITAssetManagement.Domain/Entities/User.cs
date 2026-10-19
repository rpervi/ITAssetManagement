namespace ITAssetManagement.Domain.Entities;

public sealed class User : AuditableEntity
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public required string NormalizedUsername { get; set; }
    public required string Email { get; set; }
    public required string NormalizedEmail { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginOn { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndOn { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
