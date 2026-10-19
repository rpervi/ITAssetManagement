using ITAssetManagement.Application.DTOs;

namespace ITAssetManagement.Application.Services;

public sealed class HealthService : IHealthService
{
    public HealthStatusDto GetStatus()
    {
        return new HealthStatusDto("Healthy");
    }
}
