using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITAssetManagement.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(IHealthService healthService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<HealthStatusDto> Get()
    {
        return Ok(healthService.GetStatus());
    }
}
