using SteamStorageAPI.Domain.Entities;

namespace SteamStorageAPI.Application.Interfaces.Repositories;

public interface ICurrencyRepository
{
    Task<Currency?> GetByIdAsync(int currencyId, CancellationToken ct = default);

    Task<Currency> GetBaseAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken ct = default);
    
    Task<double> GetExchangeRateAsync(int currencyId, CancellationToken ct = default);

    Task AddAsync(Currency currency, CancellationToken ct = default);

    Task UpdateAsync(Currency currency, CancellationToken ct = default);

    Task DeleteAsync(Currency currency, CancellationToken ct = default);

    Task AddDynamicAsync(CurrencyDynamic dynamic, CancellationToken ct = default);
    
    Task<bool> IsTodayAlreadyRefreshedAsync(CancellationToken ct = default);
}
