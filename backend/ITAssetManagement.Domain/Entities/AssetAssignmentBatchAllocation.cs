namespace ITAssetManagement.Domain.Entities;

public sealed class AssetAssignmentBatchAllocation : AuditableEntity
{
    public int AllocationId { get; set; }
    public int AssignmentItemId { get; set; }
    public int ReceiptItemId { get; set; }
    public int Quantity { get; set; }
    public AssetAssignmentItem? AssignmentItem { get; set; }
    public StockReceiptItem? ReceiptItem { get; set; }
}
