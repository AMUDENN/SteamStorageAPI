using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Currencies;

namespace SteamStorageAPI.Models.DTOs;

public record CurrencyResponse(
    int Id,
    int SteamCurrencyId,
    string Title,
    string Mark,
    string CultureInfo,
    decimal Price,
    DateTime DateUpdate);

public record CurrenciesResponse(
    int Count,
    IEnumerable<CurrencyResponse> Currencies);

public record CurrencyDynamicResponse(
    int Id,
    DateTime DateUpdate,
    decimal ExchangeRate);

public record CurrencyDynamicsResponse(
    IEnumerable<CurrencyDynamicResponse> Dynamic);

[Validator<GetCurrencyRequestValidator>]
public record GetCurrencyRequest(
    int Id);

[Validator<GetCurrencyDynamicsRequestValidator>]
public record GetCurrencyDynamicsRequest(
    int CurrencyId);

[Validator<PostCurrencyRequestValidator>]
public record PostCurrencyRequest(
    int SteamCurrencyId,
    string Title,
    string Mark,
    string CultureInfo);

[Validator<PutCurrencyRequestValidator>]
public record PutCurrencyRequest(
    int CurrencyId,
    string Title,
    string Mark,
    string CultureInfo);

[Validator<SetCurrencyRequestValidator>]
public record SetCurrencyRequest(
    int CurrencyId);

[Validator<DeleteCurrencyRequestValidator>]
public record DeleteCurrencyRequest(
    int CurrencyId);