using Microsoft.Extensions.Diagnostics.HealthChecks;
using SteamStorageAPI.Utilities.HealthCheck.Tools;

namespace SteamStorageAPI.Utilities.HealthCheck;

public class ApiHealthChecker : BaseHealthChecker
{
    #region Constructor

    public ApiHealthChecker(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory) : base(httpContextAccessor, httpClientFactory)
    {
    }

    #endregion Constructor

    #region Methods

    public override async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            HttpClient client = HttpClientFactory.CreateClient();
            string apiUrl = $"{HostUrl}/api/Check/GetApiStatus";
            HttpResponseMessage response = await client.GetAsync(apiUrl, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("API is working")
                : HealthCheckResult.Unhealthy($"API status code: {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"API error: {ex.Message}");
        }
    }

    #endregion Methods
}