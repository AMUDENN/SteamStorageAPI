using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SteamStorageAPI.Utilities.HealthCheckers
{
    public class ApiHealthChecker : IHealthCheck
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        #endregion Fields

        #region Constructor

        public ApiHealthChecker(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
        }

        #endregion Constructor

        #region Methods

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                HttpClient client = _httpClientFactory.CreateClient();
                string apiUrl =
                    $"{_httpContextAccessor.HttpContext?.Request.Scheme}://{_httpContextAccessor.HttpContext?.Request.Host}/api/Check/GetApiStatus";
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
}
