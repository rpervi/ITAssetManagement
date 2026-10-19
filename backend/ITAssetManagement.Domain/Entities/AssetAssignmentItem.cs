namespace ITAssetManagement.Domain.Entities;

public sealed class AssetAssignmentItem : AuditableEntity
{
    public int AssignmentItemId { get; set; }
    public int AssignmentId { get; set; }
    public int ProductId { get; set; }
    public int? AssetUnitId { get; set; }
    public int Quantity { get; set; }
    public AssetAssignment? Assignment { get; set; }
    public AssetProduct? Product { get; set; }
    public AssetUnit? AssetUnit { get; set; }
    public ICollection<AssetReturn> Returns { get; set; } = new List<AssetReturn>();
    public ICollection<AssetAssignmentBatchAllocation> BatchAllocations { get; set; } = new List<AssetAssignmentBatchAllocation>();
}
