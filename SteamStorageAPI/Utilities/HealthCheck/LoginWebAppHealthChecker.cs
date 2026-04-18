using Microsoft.Extensions.Diagnostics.HealthChecks;
using SteamStorageAPI.Utilities.Config;
using SteamStorageAPI.Utilities.HealthCheck.Tools;

namespace SteamStorageAPI.Utilities.HealthCheck;

public class LoginWebAppHealthChecker : BaseHealthChecker
{
    #region Fields

    private readonly string _loginWebAppUrl;

    #endregion Fields

    #region Constructor

    public LoginWebAppHealthChecker(
        IHttpClientFactory httpClientFactory,
        AppConfig appConfig) : base(httpClientFactory)
    {
        _loginWebAppUrl = appConfig.HealthChecks.LoginWebAppUrl;
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
            HttpResponseMessage response = await client.GetAsync($"{_loginWebAppUrl}/token/Token", cancellationToken);

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
