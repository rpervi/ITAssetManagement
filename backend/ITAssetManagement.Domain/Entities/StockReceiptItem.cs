namespace ITAssetManagement.Domain.Entities;

public sealed class StockReceiptItem : AuditableEntity
{
    public int ReceiptItemId { get; set; }
    public int ReceiptId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public AssetCondition Condition { get; set; } = AssetCondition.Good;
    public StockReceipt? Receipt { get; set; }
    public AssetProduct? Product { get; set; }
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<AssetAssignmentBatchAllocation> AssignmentBatchAllocations { get; set; } = new List<AssetAssignmentBatchAllocation>();
}
