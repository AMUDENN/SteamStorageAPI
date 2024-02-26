namespace SteamStorageAPI.Services.RefreshCurrenciesService;

public interface IRefreshCurrenciesService
{
    Task RefreshCurrenciesAsync(
        CancellationToken cancellationToken = default);
}
