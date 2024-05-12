using Microsoft.Extensions.Diagnostics.HealthChecks;
using SteamStorageAPI.Utilities.HealthCheck.Tools;

namespace SteamStorageAPI.Utilities.HealthCheck
{
    public class AdminPanelHealthChecker : BaseHealthChecker
    {
        #region Constructor

        public AdminPanelHealthChecker(
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
                string apiUrl = $"{HostUrl}/admin";
                HttpResponseMessage response = await client.GetAsync(apiUrl, cancellationToken);

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
}
