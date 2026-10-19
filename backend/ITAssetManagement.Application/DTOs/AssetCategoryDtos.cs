using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ITAssetManagement.Application.DTOs;

public sealed record AssetCategoryDto(
    int CategoryId,
    string CategoryName,
    string? Description,
    bool IsActive,
    string? CreatedBy,
    DateTime CreatedOn,
    string? ModifiedBy,
    DateTime? ModifiedOn);

public sealed record SaveAssetCategoryRequest(
    [Required, StringLength(100)] string CategoryName,
    [StringLength(500)] string? Description,
    bool IsActive = true);

public sealed record AssetCategoryStatusRequest([property: JsonRequired] bool IsActive);
