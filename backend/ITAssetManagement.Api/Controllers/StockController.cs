using System.Security.Claims;
using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Security;
using ITAssetManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAssetManagement.Api.Controllers;

[ApiController]
[Route("api/stock")]
public sealed class StockController(IStockService stockService) : ControllerBase
{
    [HttpGet("products")]
    [Authorize(Policy = AuthPolicies.ManageStock)]
    public async Task<ActionResult<IReadOnlyList<StockProductDto>>> GetProducts(
        CancellationToken cancellationToken)
    {
        return Ok(await stockService.GetProductsAsync(cancellationToken));
    }

    [HttpPost("receive/individual")]
    [Authorize(Policy = AuthPolicies.ManageStock)]
    public async Task<IActionResult> ReceiveIndividual(
        ReceiveIndividualStockRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await stockService.ReceiveIndividualAsync(
                request, CurrentActor(), cancellationToken);
            if (!result.Created)
            {
                return Ok(result.Receipt);
            }

            return CreatedAtAction(
                nameof(GetReceipt),
                new { id = result.Receipt.ReceiptId },
                result.Receipt);
        }
        catch (InvalidStockRequestException exception)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "Invalid stock receipt", exception.Message));
        }
        catch (DuplicateStockIdentifierException exception)
        {
            return Conflict(CreateProblem(StatusCodes.Status409Conflict, "Duplicate asset identifier", exception.Message));
        }
        catch (StockIdempotencyConflictException exception)
        {
            return Conflict(CreateProblem(StatusCodes.Status409Conflict, "Idempotency key conflict", exception.Message));
        }
    }

    [HttpPost("receive/bulk")]
    [Authorize(Policy = AuthPolicies.ManageStock)]
    public async Task<IActionResult> ReceiveBulk(
        ReceiveBulkStockRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await stockService.ReceiveBulkAsync(
                request, CurrentActor(), cancellationToken);
            if (!result.Created)
            {
                return Ok(result.Receipt);
            }

            return CreatedAtAction(
                nameof(GetReceipt),
                new { id = result.Receipt.ReceiptId },
                result.Receipt);
        }
        catch (InvalidStockRequestException exception)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "Invalid stock receipt", exception.Message));
        }
        catch (StockIdempotencyConflictException exception)
        {
            return Conflict(CreateProblem(StatusCodes.Status409Conflict, "Idempotency key conflict", exception.Message));
        }
    }

    [HttpGet("receipts")]
    [Authorize(Policy = AuthPolicies.ViewInventory)]
    public async Task<ActionResult<PagedStockReceiptsResponse>> GetReceipts(
        [FromQuery] string? search,
        [FromQuery] int? productId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await stockService.GetReceiptsAsync(
                search, productId, startDate, endDate, page, pageSize,
                sortBy, sortDirection, cancellationToken));
        }
        catch (InvalidStockRequestException exception)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "Invalid receipt filter", exception.Message));
        }
    }

    [HttpGet("receipts/{id:int}")]
    [Authorize(Policy = AuthPolicies.ViewInventory)]
    public async Task<ActionResult<StockReceiptResponse>> GetReceipt(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var receipt = await stockService.GetReceiptAsync(id, cancellationToken);
            return receipt is null ? NotFound() : Ok(receipt);
        }
        catch (InvalidStockRequestException exception)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "Invalid receipt ID", exception.Message));
        }
    }

    [HttpGet("availability")]
    [Authorize(Policy = AuthPolicies.ViewInventory)]
    public async Task<ActionResult<IReadOnlyList<StockAvailabilityResponse>>> GetAvailability(
        CancellationToken cancellationToken)
    {
        return Ok(await stockService.GetAvailabilityAsync(cancellationToken));
    }

    [HttpGet("availability/{productId:int}")]
    [Authorize(Policy = AuthPolicies.ViewInventory)]
    public async Task<ActionResult<StockAvailabilityResponse>> GetAvailability(
        int productId,
        CancellationToken cancellationToken)
    {
        try
        {
            var availability = await stockService.GetAvailabilityAsync(productId, cancellationToken);
            return availability is null ? NotFound() : Ok(availability);
        }
        catch (InvalidStockRequestException exception)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "Invalid product ID", exception.Message));
        }
    }

    [HttpGet("availability/{productId:int}/units")]
    [Authorize(Policy = AuthPolicies.ViewInventory)]
    public async Task<ActionResult<IReadOnlyList<StockAssetUnitResponse>>> GetUnits(
        int productId,
        CancellationToken cancellationToken)
    {
        try
        {
            var units = await stockService.GetProductUnitsAsync(productId, cancellationToken);
            return units is null ? NotFound() : Ok(units);
        }
        catch (InvalidStockRequestException exception)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "Invalid product ID", exception.Message));
        }
    }

    [HttpGet("assets/{assetCode}")]
    [Authorize(Policy = AuthPolicies.ViewInventory)]
    public async Task<ActionResult<AssetBarcodeDetailsResponse>> GetAssetByCode(
        string assetCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var asset = await stockService.GetAssetByCodeAsync(assetCode, cancellationToken);
            return asset is null ? NotFound() : Ok(asset);
        }
        catch (InvalidStockRequestException exception)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest, "Invalid asset barcode", exception.Message));
        }
    }

    [HttpPost("products/{productId:int}/convert-to-individual")]
    [Authorize(Policy = AuthPolicies.ManageStock)]
    public async Task<IActionResult> ConvertBulkStockToIndividual(
        int productId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await stockService.ConvertBulkStockToIndividualAsync(
                productId, CurrentActor(), cancellationToken));
        }
        catch (InvalidStockRequestException exception)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest, "Invalid product ID", exception.Message));
        }
        catch (BulkStockConversionException exception)
        {
            return Conflict(CreateProblem(
                StatusCodes.Status409Conflict, "Bulk stock cannot be converted", exception.Message));
        }
    }

    [HttpGet("movements")]
    [Authorize(Policy = AuthPolicies.ViewInventory)]
    public async Task<ActionResult<PagedStockMovementsResponse>> GetMovements(
        [FromQuery] int? productId,
        [FromQuery] string? movementType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await stockService.GetMovementsAsync(
                productId, movementType, startDate, endDate, page, pageSize, cancellationToken));
        }
        catch (InvalidStockRequestException exception)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "Invalid movement filter", exception.Message));
        }
    }

    private string? CurrentActor()
    {
        return User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
        };
    }
}
