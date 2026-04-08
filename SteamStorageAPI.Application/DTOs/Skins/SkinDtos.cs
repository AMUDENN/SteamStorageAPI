using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Domain.Enums;

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SteamStorageAPI.Application.DTOs.Skins;

public sealed record SkinDetailDto(
    SkinDto Skin,
    decimal CurrentPrice,
    double Change7D,
    double Change30D,
    bool IsMarked);

public sealed record SkinDynamicDto(
    int Id,
    DateTime DateUpdate,
    decimal Price);

public sealed record SkinDynamicStatsDto(
    double ChangePeriod,
    IReadOnlyList<SkinDynamicDto> Dynamic);

public sealed record SkinsFilterDto(
    int? GameId,
    string? Filter,
    SkinOrderName? OrderName,
    bool? IsAscending);

public sealed record GetSkinDynamicsDto(
    int SkinId,
    int UserId,
    DateTime StartDate,
    DateTime EndDate);

public sealed record CreateSkinDto(
    int GameId,
    string MarketHashName,
    string Title,
    string SkinIconUrl);
