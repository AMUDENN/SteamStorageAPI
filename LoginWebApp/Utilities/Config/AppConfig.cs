namespace LoginWebApp.Utilities.Config;

public class AppConfig
{
    public AppSettings App { get; set; } = new();
}

public class AppSettings
{
    public string InternalApiKey { get; set; } = string.Empty;
}
