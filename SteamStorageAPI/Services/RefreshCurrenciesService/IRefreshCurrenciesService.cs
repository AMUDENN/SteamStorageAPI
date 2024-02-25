namespace SteamStorageAPI.Services.RefreshCurrenciesService;

public interface IRefreshCurrenciesService
{
    Task RefreshCurrencies(
        CancellationToken cancellationToken = default);
}
