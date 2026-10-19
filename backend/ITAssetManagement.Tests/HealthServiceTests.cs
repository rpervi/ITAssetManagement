using ITAssetManagement.Application.Services;

namespace ITAssetManagement.Tests;

public sealed class HealthServiceTests
{
    [Fact]
    public void GetStatus_ReturnsHealthy()
    {
        var service = new HealthService();

        var result = service.GetStatus();

        Assert.Equal("Healthy", result.Status);
    }
}
