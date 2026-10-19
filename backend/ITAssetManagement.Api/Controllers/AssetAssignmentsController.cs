using System.Security.Claims;
using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Security;
using ITAssetManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAssetManagement.Api.Controllers;

[ApiController]
[Route("api/assetassignments")]
[Authorize(Policy = AuthPolicies.ViewInventory)]
public sealed class AssetAssignmentsController(IAssetAssignmentService assignmentService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AssetAssignmentPageResponse>> GetPage(
        [FromQuery] string? search,
        [FromQuery] int? employeeId,
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
            return Ok(await assignmentService.GetPageAsync(
                search, employeeId, productId, startDate, endDate, page, pageSize,
                sortBy, sortDirection, cancellationToken));
        }
        catch (InvalidAssetAssignmentException exception)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest, "Invalid assignment filter", exception.Message));
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssetAssignmentResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var assignment = await assignmentService.GetByIdAsync(id, cancellationToken);
            return assignment is null ? NotFound() : Ok(assignment);
        }
        catch (InvalidAssetAssignmentException exception)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest, "Invalid assignment ID", exception.Message));
        }
    }

    [HttpGet("employees/{employeeId:int}")]
    public async Task<ActionResult<IReadOnlyList<AssetAssignmentResponse>>> GetByEmployee(
        int employeeId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await assignmentService.GetByEmployeeAsync(employeeId, cancellationToken));
        }
        catch (InvalidAssetAssignmentException exception)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest, "Invalid employee ID", exception.Message));
        }
    }

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyList<AssetAssignmentProductDto>>> GetActiveProducts(
        CancellationToken cancellationToken)
    {
        return Ok(await assignmentService.GetActiveProductsAsync(cancellationToken));
    }

    [HttpGet("products/{productId:int}/available-units")]
    public async Task<ActionResult<IReadOnlyList<AvailableAssetUnitResponse>>> GetAvailableUnits(
        int productId,
        CancellationToken cancellationToken)
    {
        try
        {
            var units = await assignmentService.GetAvailableUnitsAsync(productId, cancellationToken);
            return units is null ? NotFound() : Ok(units);
        }
        catch (InvalidAssetAssignmentException exception)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest, "Invalid product ID", exception.Message));
        }
    }

    [HttpGet("products/{productId:int}/available-stock")]
    public async Task<ActionResult<AvailableStockResponse>> GetAvailableStock(
        int productId,
        CancellationToken cancellationToken)
    {
        try
        {
            var stock = await assignmentService.GetAvailableStockAsync(productId, cancellationToken);
            return stock is null ? NotFound() : Ok(stock);
        }
        catch (InvalidAssetAssignmentException exception)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest, "Invalid product ID", exception.Message));
        }
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.AssignAndReturnAssets)]
    public async Task<ActionResult<AssetAssignmentResponse>> Create(
        CreateAssetAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await assignmentService.CreateAsync(
                request, CurrentActor(), cancellationToken);
            if (!result.Created)
            {
                return Ok(result.Assignment);
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Assignment.AssignmentId },
                result.Assignment);
        }
        catch (AssetAssignmentIdempotencyConflictException exception)
        {
            return Conflict(CreateProblem(
                StatusCodes.Status409Conflict, "Idempotency key conflict", exception.Message));
        }
        catch (AssetAssignmentConflictException exception)
        {
            return Conflict(CreateProblem(
                StatusCodes.Status409Conflict, "Assignment conflict", exception.Message));
        }
        catch (InvalidAssetAssignmentException exception)
        {
            return BadRequest(CreateProblem(
                StatusCodes.Status400BadRequest, "Invalid assignment", exception.Message));
        }
    }

    private string? CurrentActor() =>
        User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;

    private static ProblemDetails CreateProblem(int status, string title, string detail) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail,
        };
}
