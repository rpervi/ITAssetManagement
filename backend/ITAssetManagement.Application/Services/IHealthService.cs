using ITAssetManagement.Application.DTOs;

namespace ITAssetManagement.Application.Services;

public interface IHealthService
{
    HealthStatusDto GetStatus();
}
