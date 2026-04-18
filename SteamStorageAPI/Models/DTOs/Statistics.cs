using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Statistics;

namespace SteamStorageAPI.Models.DTOs;

public record InvestmentSumResponse(
    double TotalSum,
    double PercentageGrowth);

public record FinancialGoalResponse(
    double FinancialGoal,
    double PercentageCompletion);

public record ActiveStatisticResponse(
    int Count,
    double CurrentSum,
    double PercentageGrowth);

public record ArchiveStatisticResponse(
    int Count,
    double SoldSum,
    double PercentageGrowth);

public record InventoryStatisticResponse(
    int Count,
    double Sum,
    IEnumerable<InventoryGameStatisticResponse> Games);

public record InventoryGameStatisticResponse(
    string GameTitle,
    double Percentage,
    int Count);

public record ItemsCountResponse(
    int Count);

public record UsersCountByCurrencyItemResponse(
    int CurrencyId,
    string CurrencyTitle,
    int UsersCount);

public record UsersCountByCurrencyResponse(
    IEnumerable<UsersCountByCurrencyItemResponse> Items);

[Validator<GetItemsCountByGameRequestValidator>]
public record GetItemsCountByGameRequest(
    int GameId);