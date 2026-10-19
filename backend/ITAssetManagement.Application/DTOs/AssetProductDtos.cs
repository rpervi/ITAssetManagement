using System.ComponentModel.DataAnnotations;

namespace ITAssetManagement.Application.DTOs;

public sealed record AssetProductDto(
    int ProductId,
    int CategoryId,
    string CategoryName,
    string ProductName,
    string Brand,
    string? Model,
    string TrackingType,
    int MinimumStock,
    bool IsActive,
    string? CreatedBy,
    DateTime CreatedOn,
    string? ModifiedBy,
    DateTime? ModifiedOn);

public sealed record SaveAssetProductRequest(
    [Required, Range(1, int.MaxValue)] int CategoryId,
    [Required, StringLength(150)] string ProductName,
    [Required, StringLength(100)] string Brand,
    [StringLength(100)] string? Model,
    [Required, StringLength(20)] string TrackingType,
    [Range(0, int.MaxValue)] int MinimumStock,
    bool IsActive = true);

public sealed record AssetProductStatusRequest([property: System.Text.Json.Serialization.JsonRequired] bool IsActive);
