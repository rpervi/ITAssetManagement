namespace ITAssetManagement.Domain.Entities;

public sealed class StockMovement : AuditableEntity
{
    public int MovementId { get; set; }
    public int ProductId { get; set; }
    public int? ReceiptItemId { get; set; }
    public int? AssetUnitId { get; set; }
    public MovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public DateTime MovementDate { get; set; }
    public int? ReferenceId { get; set; }
    public string? Remarks { get; set; }
    public AssetProduct? Product { get; set; }
    public StockReceiptItem? ReceiptItem { get; set; }
    public AssetUnit? AssetUnit { get; set; }
}
