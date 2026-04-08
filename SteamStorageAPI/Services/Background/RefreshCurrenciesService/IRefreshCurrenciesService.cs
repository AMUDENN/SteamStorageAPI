namespace SteamStorageAPI.Services.Background.RefreshCurrenciesService;

public interface IRefreshCurrenciesService
{
    Task RefreshCurrenciesAsync(
        CancellationToken cancellationToken = default);
}