namespace ITAssetManagement.Domain.Entities;

public sealed class AssetUnit : AuditableEntity
{
    public int AssetUnitId { get; set; }
    public int ProductId { get; set; }
    public required string AssetCode { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public decimal PurchaseCost { get; set; }
    public AssetStatus Status { get; set; }
    public AssetCondition Condition { get; set; }
    public AssetProduct? Product { get; set; }
    public ICollection<AssetAssignmentItem> AssignmentItems { get; set; } = new List<AssetAssignmentItem>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
