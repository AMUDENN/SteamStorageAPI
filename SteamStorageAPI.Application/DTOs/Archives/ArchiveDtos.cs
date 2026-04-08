using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Domain.Enums;

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SteamStorageAPI.Application.DTOs.Archives;

public sealed record ArchiveDto(
    int Id,
    int GroupId,
    SkinDto Skin,
    DateTime BuyDate,
    DateTime SoldDate,
    int Count,
    decimal BuyPrice,
    decimal SoldPrice,
    decimal SoldSum,
    double Change,
    string? Description);

public sealed record ArchivesFilterDto(
    int? GroupId,
    int? GameId,
    string? Filter,
    ArchiveOrderName? OrderName,
    bool? IsAscending);

public sealed record ArchiveStatisticDto(
    int ArchivesCount,
    decimal InvestmentSum,
    decimal SoldSum);

public sealed record CreateArchiveDto(
    int UserId,
    int GroupId,
    int SkinId,
    int Count,
    decimal BuyPrice,
    decimal SoldPrice,
    DateTime BuyDate,
    DateTime SoldDate,
    string? Description);

public sealed record UpdateArchiveDto(
    int ArchiveId,
    int UserId,
    int GroupId,
    int SkinId,
    int Count,
    decimal BuyPrice,
    decimal SoldPrice,
    DateTime BuyDate,
    DateTime SoldDate,
    string? Description);
