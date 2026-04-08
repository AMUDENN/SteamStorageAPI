using SteamStorageAPI.Domain.Enums;

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SteamStorageAPI.Application.DTOs.ActiveGroups;

public sealed record ActiveGroupDto(
    int Id,
    string Title,
    string? Description,
    string Colour,
    decimal? GoalSum,
    double? GoalSumCompletion,
    int Count,
    decimal BuySum,
    decimal CurrentSum,
    double Change,
    DateTime DateCreation);

public sealed record ActiveGroupDynamicDto(
    int Id,
    DateTime DateUpdate,
    decimal Sum);

public sealed record ActiveGroupDynamicStatsDto(
    double ChangePeriod,
    IReadOnlyList<ActiveGroupDynamicDto> Dynamic);

public sealed record ActiveGroupsStatisticDto(
    int ActivesCount,
    decimal InvestmentSum,
    decimal CurrentSum,
    IReadOnlyList<GameShareDto> GameCount,
    IReadOnlyList<GameShareDto> GameInvestmentSum,
    IReadOnlyList<GameShareDto> GameCurrentSum);

public sealed record GameShareDto(
    string GameTitle,
    double Percentage,
    decimal Value);

public sealed record CreateActiveGroupDto(
    int UserId,
    string Title,
    string? Description,
    string? Colour,
    decimal? GoalSum);

public sealed record UpdateActiveGroupDto(
    int GroupId,
    int UserId,
    string Title,
    string? Description,
    string? Colour,
    decimal? GoalSum);

public sealed record GetActiveGroupsFilterDto(
    ActiveGroupOrderName? OrderName,
    bool? IsAscending);

public sealed record GetActiveGroupDynamicDto(
    int GroupId,
    int UserId,
    DateTime StartDate,
    DateTime EndDate);
