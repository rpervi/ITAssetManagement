namespace ITAssetManagement.Domain.Entities;

public sealed class AssetAssignment : AuditableEntity
{
    public int AssignmentId { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public int EmployeeId { get; set; }
    public DateTime AssignedDate { get; set; }
    public required string AssignedBy { get; set; }
    public string? Remarks { get; set; }
    public Employee? Employee { get; set; }
    public ICollection<AssetAssignmentItem> Items { get; set; } = new List<AssetAssignmentItem>();
}
