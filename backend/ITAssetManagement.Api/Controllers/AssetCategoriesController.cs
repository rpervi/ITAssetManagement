using System.Security.Claims;
using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Security;
using ITAssetManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAssetManagement.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.ManageAssets)]
[Route("api/assetcategories")]
public sealed class AssetCategoriesController(IAssetCategoryService assetCategoryService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssetCategoryDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await assetCategoryService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetCategoryDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var category = await assetCategoryService.GetByIdAsync(id, cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<AssetCategoryDto>> Create(
        SaveAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await assetCategoryService.CreateAsync(
                request,
                CurrentActor(),
                cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, category);
        }
        catch (DuplicateAssetCategoryNameException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate category name",
                Detail = exception.Message,
            });
        }
        catch (InvalidAssetCategoryException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid category",
                Detail = exception.Message,
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetCategoryDto>> Update(
        int id,
        SaveAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await assetCategoryService.UpdateAsync(
                id,
                request,
                CurrentActor(),
                cancellationToken);
            return category is null ? NotFound() : Ok(category);
        }
        catch (DuplicateAssetCategoryNameException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate category name",
                Detail = exception.Message,
            });
        }
        catch (InvalidAssetCategoryException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid category",
                Detail = exception.Message,
            });
        }
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<AssetCategoryDto>> SetStatus(
        int id,
        AssetCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        var category = await assetCategoryService.SetStatusAsync(
            id,
            request.IsActive,
            CurrentActor(),
            cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    private string? CurrentActor()
    {
        return User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
    }
}
