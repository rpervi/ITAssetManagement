namespace ITAssetManagement.Domain.Entities;

public sealed class StockAdjustment : AuditableEntity
{
    public int AdjustmentId { get; set; }
    public int ProductId { get; set; }
    public int QuantityChange { get; set; }
    public required string Reason { get; set; }
    public DateTime AdjustmentDate { get; set; }
    public required string ApprovedBy { get; set; }
    public AssetProduct? Product { get; set; }
}
