using Microsoft.Extensions.Diagnostics.HealthChecks;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Utilities.Steam;

namespace SteamStorageAPI.Utilities.HealthCheck
{
    public class SteamProfileHealthChecker : IHealthCheck
    {
        #region Fields

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public SteamProfileHealthChecker(IHttpClientFactory httpClientFactory, SteamStorageContext context)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
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
                User? user = _context.Users.FirstOrDefault();
                if (user is null) return HealthCheckResult.Healthy("Cannot check HealthStatus");
                string profileUrl = SteamApi.GetUserInfoUrl(user.SteamId);
                HttpResponseMessage response = await client.GetAsync(profileUrl, cancellationToken);

                return response.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy("Steam Profiles is working")
                    : HealthCheckResult.Unhealthy($"Steam Profiles status code: {response.StatusCode}.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Steam Profiles error: {ex.Message}");
            }
        }

        #endregion Methods
    }
}
