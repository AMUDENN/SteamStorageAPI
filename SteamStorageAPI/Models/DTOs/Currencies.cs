using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Currencies;

namespace SteamStorageAPI.Models.DTOs;

public record CurrencyResponse(
    int Id,
    int SteamCurrencyId,
    string Title,
    string Mark,
    string CultureInfo,
    double Price,
    DateTime DateUpdate);

public record CurrenciesResponse(
    int Count,
    IEnumerable<CurrencyResponse> Currencies);

[Validator<GetCurrencyRequestValidator>]
public record GetCurrencyRequest(
    int Id);

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