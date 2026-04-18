using Microsoft.Extensions.Diagnostics.HealthChecks;
using SteamStorageAPI.Utilities.Config;
using SteamStorageAPI.Utilities.HealthCheck.Tools;

namespace SteamStorageAPI.Utilities.HealthCheck;

public class AdminPanelHealthChecker : BaseHealthChecker
{
    #region Fields

    private readonly string _adminPanelUrl;

    #endregion Fields

    #region Constructor

    public AdminPanelHealthChecker(
        IHttpClientFactory httpClientFactory,
        AppConfig appConfig) : base(httpClientFactory)
    {
        _adminPanelUrl = appConfig.HealthChecks.AdminPanelUrl;
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
            HttpResponseMessage response = await client.GetAsync($"{_adminPanelUrl}/admin", cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("AdminPanel is working")
                : HealthCheckResult.Unhealthy($"AdminPanel status code: {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"AdminPanel error: {ex.Message}");
        }
    }

    #endregion Methods
}
