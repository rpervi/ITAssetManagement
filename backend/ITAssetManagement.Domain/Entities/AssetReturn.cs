namespace ITAssetManagement.Domain.Entities;

public sealed class AssetReturn : AuditableEntity
{
    public int ReturnId { get; set; }
    public int AssignmentItemId { get; set; }
    public int ReturnQuantity { get; set; }
    public DateTime ReturnDate { get; set; }
    public AssetCondition ReturnedCondition { get; set; }
    public string? Remarks { get; set; }
    public AssetAssignmentItem? AssignmentItem { get; set; }
}
