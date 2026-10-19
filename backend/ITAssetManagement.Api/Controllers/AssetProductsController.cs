using System.Security.Claims;
using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Security;
using ITAssetManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAssetManagement.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.ManageAssets)]
[Route("api/assetproducts")]
public sealed class AssetProductsController(IAssetProductService assetProductService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssetProductDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await assetProductService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetProductDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await assetProductService.GetByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<AssetProductDto>> Create(
        SaveAssetProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await assetProductService.CreateAsync(
                request, CurrentActor(), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, product);
        }
        catch (DuplicateAssetProductException exception)
        {
            return Conflict(Problem(StatusCodes.Status409Conflict, "Duplicate product", exception.Message));
        }
        catch (InvalidAssetProductException exception)
        {
            return BadRequest(Problem(StatusCodes.Status400BadRequest, "Invalid product", exception.Message));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssetProductDto>> Update(
        int id,
        SaveAssetProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await assetProductService.UpdateAsync(
                id, request, CurrentActor(), cancellationToken);
            return product is null ? NotFound() : Ok(product);
        }
        catch (DuplicateAssetProductException exception)
        {
            return Conflict(Problem(StatusCodes.Status409Conflict, "Duplicate product", exception.Message));
        }
        catch (InvalidAssetProductException exception)
        {
            return BadRequest(Problem(StatusCodes.Status400BadRequest, "Invalid product", exception.Message));
        }
        catch (AssetProductHistoryException exception)
        {
            return Conflict(Problem(StatusCodes.Status409Conflict, "Product has history", exception.Message));
        }
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<AssetProductDto>> SetStatus(
        int id,
        AssetProductStatusRequest request,
        CancellationToken cancellationToken)
    {
        var product = await assetProductService.SetStatusAsync(
            id, request.IsActive, CurrentActor(), cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    private string? CurrentActor()
    {
        return User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
    }

    private static ProblemDetails Problem(int status, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
        };
    }
}
