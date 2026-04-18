using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Services.Domain.CurrencyService;

public interface ICurrencyService
{
    Task<double> GetCurrencyExchangeRateAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<CurrenciesResponse> GetCurrenciesAsync(
        CancellationToken cancellationToken = default);

    Task<CurrencyResponse> GetCurrencyAsync(
        GetCurrencyRequest request,
        CancellationToken cancellationToken = default);

    Task<CurrencyResponse> GetCurrentCurrencyAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task PostCurrencyAsync(
        PostCurrencyRequest request,
        CancellationToken cancellationToken = default);

    Task PutCurrencyInfoAsync(
        PutCurrencyRequest request,
        CancellationToken cancellationToken = default);

    Task SetCurrencyAsync(
        User user,
        SetCurrencyRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteCurrencyAsync(
        DeleteCurrencyRequest request,
        CancellationToken cancellationToken = default);

    Task<CurrencyDynamicsResponse> GetCurrencyDynamicsAsync(
        GetCurrencyDynamicsRequest request,
        CancellationToken cancellationToken = default);
}