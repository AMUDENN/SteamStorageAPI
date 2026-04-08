using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Domain.Enums;

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SteamStorageAPI.Application.DTOs.Actives;

public sealed record ActiveDto(
    int Id,
    int GroupId,
    SkinDto Skin,
    DateTime BuyDate,
    int Count,
    decimal BuyPrice,
    decimal CurrentPrice,
    decimal CurrentSum,
    decimal? GoalPrice,
    double? GoalPriceCompletion,
    double Change,
    string? Description);

public sealed record ActivesFilterDto(
    int? GroupId,
    int? GameId,
    string? Filter,
    ActiveOrderName? OrderName,
    bool? IsAscending);

public sealed record ActiveStatisticDto(
    int ActivesCount,
    decimal InvestmentSum,
    decimal CurrentSum);

public sealed record CreateActiveDto(
    int UserId,
    int GroupId,
    int SkinId,
    int Count,
    decimal BuyPrice,
    decimal? GoalPrice,
    DateTime BuyDate,
    string? Description);

public sealed record UpdateActiveDto(
    int ActiveId,
    int UserId,
    int GroupId,
    int SkinId,
    int Count,
    decimal BuyPrice,
    decimal? GoalPrice,
    DateTime BuyDate,
    string? Description);

public sealed record SoldActiveDto(
    int ActiveId,
    int UserId,
    int ArchiveGroupId,
    decimal SoldPrice,
    DateTime SoldDate);
