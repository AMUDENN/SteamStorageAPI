namespace SteamStorageAPI.Utilities.Config;

public class AppConfig
{
    public AppSettings App { get; set; } = new();
    public JwtConfig Jwt { get; set; } = new();
    public SteamConfig Steam { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public HealthChecksConfig HealthChecks { get; set; } = new();
    public RateLimitConfig RateLimit { get; set; } = new();
    public BackgroundServicesConfig BackgroundServices { get; set; } = new();
}

// ─── App ────────────────────────────────────────────────────────────────────

public class AppSettings
{
    public string TokenAddress { get; set; } = string.Empty;
    public string DateFormat { get; set; } = "dd.MM.yyyy";
    public string? PublicHost { get; set; }
    public string InternalApiKey { get; set; } = string.Empty;
}

// ─── JWT ────────────────────────────────────────────────────────────────────

public class JwtConfig
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresDays { get; set; } = 1;
}

// ─── Steam ──────────────────────────────────────────────────────────────────

public class SteamConfig
{
    public string ApiKey { get; set; } = string.Empty;
}

// ─── Database ───────────────────────────────────────────────────────────────

public class DatabaseConfig
{
    public string SteamStorage { get; set; } = string.Empty;
}

// ─── HealthChecks ───────────────────────────────────────────────────────────

public class HealthChecksConfig
{
    public string ApiUrl { get; set; } = string.Empty;
    public string AdminPanelUrl { get; set; } = string.Empty;
    public string LoginWebAppUrl { get; set; } = string.Empty;
}

// ─── RateLimit ──────────────────────────────────────────────────────────────

public class RateLimitConfig
{
    public bool EnableEndpointRateLimiting { get; set; } = true;
    public bool StackBlockedRequests { get; set; }
    public int HttpStatusCode { get; set; } = 429;
    public string RealIpHeader { get; set; } = "X-Forwarded-For";
    public string ClientIdHeader { get; set; } = "X-ClientId";
    public List<RateLimitRule> Rules { get; set; } = [];
}

public class RateLimitRule
{
    public string Endpoint { get; set; } = "*";
    public string Period { get; set; } = "1s";
    public int Limit { get; set; } = 20;
}

// ─── BackgroundServices ─────────────────────────────────────────────────────

public class BackgroundServicesConfig
{
    public QuartzJobConfig RefreshSkinDynamicsJob { get; set; } = new();
    public RefreshCurrenciesConfig RefreshCurrencies { get; set; } = new();
    public QuartzJobConfig RefreshActiveGroupsDynamicsJob { get; set; } = new();
}

public class QuartzJobConfig
{
    public string CronSchedule { get; set; } = "0 0 0 * * ?";
    public int ErrorDelayMs { get; set; } = 30 * 60 * 1000;
}

public class RefreshCurrenciesConfig : QuartzJobConfig
{
    public RefreshCurrenciesConfig()
    {
        CronSchedule = "0 0 1 * * ?";
    }
}