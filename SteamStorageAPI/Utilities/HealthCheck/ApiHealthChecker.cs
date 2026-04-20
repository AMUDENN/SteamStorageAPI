using Microsoft.Extensions.Diagnostics.HealthChecks;
using SteamStorageAPI.Utilities.HealthCheck.Tools;

namespace SteamStorageAPI.Utilities.HealthCheck;

public class ApiHealthChecker : BaseHealthChecker
{
    #region Constructor

    public ApiHealthChecker(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory) {}

    #endregion Constructor

    #region Methods

    public override Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("API is working"));
    }

    #endregion Methods
}