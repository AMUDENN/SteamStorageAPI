using Microsoft.Extensions.Diagnostics.HealthChecks;
using SteamStorageAPI.Utilities.HealthCheck.Tools;

namespace SteamStorageAPI.Utilities.HealthCheck
{
    public class LoginWebAppHealthChecker : BaseHealthChecker
    {
        #region Constructor

        public LoginWebAppHealthChecker(
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
                string apiUrl = $"{HostUrl}/token/Token";
                HttpResponseMessage response = await client.GetAsync(apiUrl, cancellationToken);

                return response.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy("LoginWebApp is working")
                    : HealthCheckResult.Unhealthy($"LoginWebApp status code: {response.StatusCode}.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"LoginWebApp error: {ex.Message}");
            }
        }

        #endregion Methods
    }
}
