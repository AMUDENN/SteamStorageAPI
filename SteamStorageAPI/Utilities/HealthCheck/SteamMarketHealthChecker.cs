using Microsoft.Extensions.Diagnostics.HealthChecks;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Utilities.Steam;

namespace SteamStorageAPI.Utilities.HealthCheck
{
    public class SteamMarketHealthChecker : IHealthCheck
    {
        #region Fields

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public SteamMarketHealthChecker(IHttpClientFactory httpClientFactory, SteamStorageContext context)
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
                Skin? skin = _context.Skins.FirstOrDefault();
                if (skin is null) return HealthCheckResult.Healthy("Cannot check HealthStatus");
                string marketUrl = SteamApi.GetSkinInfoUrl(skin.MarketHashName);
                HttpResponseMessage response = await client.GetAsync(marketUrl, cancellationToken);

                return response.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy("Steam Market is working")
                    : HealthCheckResult.Unhealthy($"Steam Market status code: {response.StatusCode}.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Steam Market error: {ex.Message}");
            }
        }

        #endregion Methods
    }
}
