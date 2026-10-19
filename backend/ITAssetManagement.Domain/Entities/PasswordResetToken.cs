namespace ITAssetManagement.Domain.Entities;

public sealed class PasswordResetToken : AuditableEntity
{
    public int TokenId { get; set; }
    public int UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresOn { get; set; }
    public DateTime? UsedOn { get; set; }
    public User? User { get; set; }
}
