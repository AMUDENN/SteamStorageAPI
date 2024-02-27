using SteamStorageAPI.DBEntities;

namespace SteamStorageAPI.Services.CurrencyService;

public interface ICurrencyService
{
    Task<double> GetCurrencyExchangeRateAsync(
        User user,
        CancellationToken cancellationToken = default);
}
