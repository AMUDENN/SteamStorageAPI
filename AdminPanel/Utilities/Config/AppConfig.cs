namespace AdminPanel.Utilities.Config;

public class AppConfig
{
    public AppSettings App { get; set; } = new();
}

public class AppSettings
{
    public int ClientTimeout { get; set; } = 15;
    public string ClientName { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string ServerAddress { get; set; } = string.Empty;
    public string ApiAddress { get; set; } = string.Empty;
    public string TokenHubEndpoint { get; set; } = string.Empty;
}
