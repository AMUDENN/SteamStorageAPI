using SteamStorageAPI.Application.Interfaces.Repositories;
using SteamStorageAPI.Domain.Entities;
using SteamStorageAPI.Domain.Exceptions;

namespace SteamStorageAPI.Application.UseCases.Currencies;

public sealed class CurrencyService
{
    #region Fields

    private readonly ICurrencyRepository _currencyRepository;

    #endregion

    #region Constructor

    public CurrencyService(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    #endregion

    #region Methods

    public async Task<IReadOnlyList<Currency>> GetAllAsync(CancellationToken ct = default) =>
        await _currencyRepository.GetAllAsync(ct);

    public async Task<Currency> GetByIdAsync(int currencyId, CancellationToken ct = default) =>
        await _currencyRepository.GetByIdAsync(currencyId, ct)
        ?? throw new NotFoundException("Currency", currencyId);

    public async Task CreateAsync(
        int steamCurrencyId, string title, string mark, string cultureInfo,
        CancellationToken ct = default)
    {
        await _currencyRepository.AddAsync(
            new Currency(steamCurrencyId, title, mark, cultureInfo), ct);
    }

    public async Task UpdateAsync(
        int currencyId, string title, string mark, string cultureInfo,
        CancellationToken ct = default)
    {
        Currency currency = await _currencyRepository.GetByIdAsync(currencyId, ct)
            ?? throw new NotFoundException("Currency", currencyId);

        currency.Update(title, mark, cultureInfo);
        await _currencyRepository.UpdateAsync(currency, ct);
    }

    public async Task DeleteAsync(int currencyId, CancellationToken ct = default)
    {
        Currency currency = await _currencyRepository.GetByIdAsync(currencyId, ct)
            ?? throw new NotFoundException("Currency", currencyId);

        await _currencyRepository.DeleteAsync(currency, ct);
    }

    #endregion
}
