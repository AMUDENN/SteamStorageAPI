using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SteamStorageAPI.Utilities.HealthCheck.Tools;

public abstract class BaseHealthChecker : IHealthCheck
{
    #region Fields

    private readonly IHttpContextAccessor _httpContextAccessor;
    protected readonly IHttpClientFactory HttpClientFactory;

    #endregion Fields
    
    #region Properties

    protected string HostUrl
    {
        get => $"{_httpContextAccessor.HttpContext?.Request.Scheme}://{_httpContextAccessor.HttpContext?.Request.Host}";
    }
    
    #endregion Properties

    #region Constructor

    protected BaseHealthChecker(
        IHttpContextAccessor httpContextAccessor, 
        IHttpClientFactory httpClientFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        HttpClientFactory = httpClientFactory;
    }

    #endregion Constructor

    #region Methods

    public abstract Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default);

    #endregion Methods
}