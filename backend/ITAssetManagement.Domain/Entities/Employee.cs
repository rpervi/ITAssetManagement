namespace ITAssetManagement.Domain.Entities;

public sealed class Employee : AuditableEntity
{
    public int EmployeeId { get; set; }
    public required string EmployeeCode { get; set; }
    public required string EmployeeName { get; set; }
    public required string Email { get; set; }
    public required string Department { get; set; }
    public string? Designation { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public DateTime? JoiningDate { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<AssetAssignment> AssetAssignments { get; set; } = new List<AssetAssignment>();
}
