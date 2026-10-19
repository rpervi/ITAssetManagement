using System.Security.Claims;
using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Security;
using ITAssetManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAssetManagement.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.ViewEmployees)]
[Route("api/employees")]
public sealed class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EmployeePageResponse>> GetPage(
        [FromQuery] string? search,
        [FromQuery] string? department,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await employeeService.GetPageAsync(
                search, department, isActive, page, pageSize, sortBy, sortDirection,
                cancellationToken));
        }
        catch (InvalidEmployeeException exception)
        {
            return BadRequest(Problem(StatusCodes.Status400BadRequest, "Invalid employee filter", exception.Message));
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<EmployeeOptionDto>>> GetActive(
        CancellationToken cancellationToken)
    {
        return Ok(await employeeService.GetActiveAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var employee = await employeeService.GetByIdAsync(id, cancellationToken);
            return employee is null ? NotFound() : Ok(employee);
        }
        catch (InvalidEmployeeException exception)
        {
            return BadRequest(Problem(StatusCodes.Status400BadRequest, "Invalid employee ID", exception.Message));
        }
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ManageUsers)]
    public async Task<ActionResult<EmployeeDto>> Create(
        SaveEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var employee = await employeeService.CreateAsync(
                request, CurrentActor(), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = employee.EmployeeId }, employee);
        }
        catch (DuplicateEmployeeException exception)
        {
            return Conflict(Problem(StatusCodes.Status409Conflict, "Duplicate employee", exception.Message));
        }
        catch (InvalidEmployeeException exception)
        {
            return BadRequest(Problem(StatusCodes.Status400BadRequest, "Invalid employee", exception.Message));
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthPolicies.ManageUsers)]
    public async Task<ActionResult<EmployeeDto>> Update(
        int id,
        SaveEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var employee = await employeeService.UpdateAsync(
                id, request, CurrentActor(), cancellationToken);
            return employee is null ? NotFound() : Ok(employee);
        }
        catch (DuplicateEmployeeException exception)
        {
            return Conflict(Problem(StatusCodes.Status409Conflict, "Duplicate employee", exception.Message));
        }
        catch (InvalidEmployeeException exception)
        {
            return BadRequest(Problem(StatusCodes.Status400BadRequest, "Invalid employee", exception.Message));
        }
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = AuthPolicies.ManageUsers)]
    public async Task<ActionResult<EmployeeDto>> SetStatus(
        int id,
        EmployeeStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var employee = await employeeService.SetStatusAsync(
                id, request.IsActive, CurrentActor(), cancellationToken);
            return employee is null ? NotFound() : Ok(employee);
        }
        catch (InvalidEmployeeException exception)
        {
            return BadRequest(Problem(StatusCodes.Status400BadRequest, "Invalid employee ID", exception.Message));
        }
    }

    private string? CurrentActor()
    {
        return User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
    }

    private static ProblemDetails Problem(int status, string title, string detail)
    {
        return new ProblemDetails { Status = status, Title = title, Detail = detail };
    }
}
