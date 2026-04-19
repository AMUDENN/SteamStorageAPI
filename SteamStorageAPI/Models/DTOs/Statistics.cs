using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Statistics;

namespace SteamStorageAPI.Models.DTOs;

public record InvestmentSumResponse(
    decimal TotalSum,
    decimal PercentageGrowth);

public record FinancialGoalResponse(
    decimal FinancialGoal,
    decimal PercentageCompletion);

public record ActiveStatisticResponse(
    int Count,
    decimal CurrentSum,
    decimal PercentageGrowth);

public record ArchiveStatisticResponse(
    int Count,
    decimal SoldSum,
    decimal PercentageGrowth);

public record InventoryStatisticResponse(
    int Count,
    decimal Sum,
    IEnumerable<InventoryGameStatisticResponse> Games);

public record InventoryGameStatisticResponse(
    string GameTitle,
    decimal Percentage,
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