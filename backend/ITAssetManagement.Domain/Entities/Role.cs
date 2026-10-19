namespace ITAssetManagement.Domain.Entities;

public sealed class Role
{
    public int RoleId { get; set; }
    public required string RoleName { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
