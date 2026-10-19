namespace ITAssetManagement.Domain.Entities;

public sealed class UserSession : AuditableEntity
{
    public Guid SessionId { get; set; }
    public int UserId { get; set; }
    public required string RefreshTokenHash { get; set; }
    public DateTime ExpiresOn { get; set; }
    public DateTime? RevokedOn { get; set; }
    public User? User { get; set; }
}
