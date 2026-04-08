// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SteamStorageAPI.Application.DTOs.Statistics;

public sealed record InvestmentSumDto(
    decimal TotalSum,
    double PercentageGrowth);

public sealed record FinancialGoalDto(
    decimal FinancialGoal,
    double PercentageCompletion);

public sealed record ActivesStatisticDto(
    int Count,
    decimal CurrentSum,
    double PercentageGrowth);

public sealed record ArchivesStatisticDto(
    int Count,
    decimal SoldSum,
    double PercentageGrowth);

public sealed record InventoryStatisticDto(
    int Count,
    decimal Sum,
    IReadOnlyList<InventoryGameStatisticDto> Games);

public sealed record InventoryGameStatisticDto(
    string GameTitle,
    double Percentage,
    int Count);

public sealed record ItemsCountDto(int Count);
