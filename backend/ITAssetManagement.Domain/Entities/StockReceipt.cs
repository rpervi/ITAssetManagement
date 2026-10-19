namespace ITAssetManagement.Domain.Entities;

public sealed class StockReceipt : AuditableEntity
{
    public int ReceiptId { get; set; }
    public required string ReceiptNumber { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string? Supplier { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Remarks { get; set; }
    public ICollection<StockReceiptItem> Items { get; set; } = new List<StockReceiptItem>();
}
