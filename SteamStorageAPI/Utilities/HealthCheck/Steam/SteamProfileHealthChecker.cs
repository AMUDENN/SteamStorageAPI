using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;

namespace SteamStorageAPI.Utilities.HealthCheck.Steam;

public class SteamProfileHealthChecker : IHealthCheck
{
    #region Fields

    private readonly ISteamApiUrlBuilder _steamApiUrlBuilder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public SteamProfileHealthChecker(
        ISteamApiUrlBuilder steamApiUrlBuilder,
        IHttpClientFactory httpClientFactory,
        SteamStorageContext context)
    {
        _steamApiUrlBuilder = steamApiUrlBuilder;
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
            User? user = await _context.Users.FirstOrDefaultAsync(cancellationToken);
            if (user is null) return HealthCheckResult.Healthy("Cannot check HealthStatus");
            string profileUrl = _steamApiUrlBuilder.GetUserInfoUrl(user.SteamId);
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