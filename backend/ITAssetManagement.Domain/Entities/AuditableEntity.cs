namespace ITAssetManagement.Domain.Entities;

public abstract class AuditableEntity
{
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
