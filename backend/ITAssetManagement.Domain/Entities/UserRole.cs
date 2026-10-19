namespace ITAssetManagement.Domain.Entities;

public sealed class UserRole : AuditableEntity
{
    public int UserRoleId { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public User? User { get; set; }
    public Role? Role { get; set; }
}
