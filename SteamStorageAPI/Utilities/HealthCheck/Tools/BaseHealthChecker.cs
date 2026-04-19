using Microsoft.Extensions.Diagnostics.HealthChecks;
using SteamStorageAPI.Utilities.Config;

namespace SteamStorageAPI.Utilities.HealthCheck.Tools;

public abstract class BaseHealthChecker : IHealthCheck
{
    #region Fields

    protected readonly IHttpClientFactory HttpClientFactory;

    #endregion Fields

    #region Constructor

    protected BaseHealthChecker(
        IHttpClientFactory httpClientFactory)
    {
        HttpClientFactory = httpClientFactory;
    }

    #endregion Constructor

    #region Methods

    public abstract Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default);

    #endregion Methods
}