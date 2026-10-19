namespace ITAssetManagement.Domain.Entities;

public sealed class AssetCategory : AuditableEntity
{
    public int CategoryId { get; set; }
    public required string CategoryName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<AssetProduct> AssetProducts { get; set; } = new List<AssetProduct>();
}
