namespace ITAssetManagement.Domain.Entities;

public sealed class AssetProduct : AuditableEntity
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public required string ProductName { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public TrackingType TrackingType { get; set; }
    public int MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
    public AssetCategory? Category { get; set; }
    public ICollection<AssetUnit> AssetUnits { get; set; } = new List<AssetUnit>();
    public ICollection<StockReceiptItem> StockReceiptItems { get; set; } = new List<StockReceiptItem>();
    public ICollection<AssetAssignmentItem> AssetAssignmentItems { get; set; } = new List<AssetAssignmentItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public ICollection<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();
}
