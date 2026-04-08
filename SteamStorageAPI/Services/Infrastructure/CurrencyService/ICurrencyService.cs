using SteamStorageAPI.Models.DBEntities;

namespace SteamStorageAPI.Services.Infrastructure.CurrencyService;

public interface ICurrencyService
{
    Task<double> GetCurrencyExchangeRateAsync(
        User user,
        CancellationToken cancellationToken = default);
}