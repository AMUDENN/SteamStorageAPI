using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Domain.Enums;

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace SteamStorageAPI.Application.DTOs.Inventory;

public sealed record InventoryItemDto(
    int Id,
    SkinDto Skin,
    int Count,
    decimal CurrentPrice,
    decimal CurrentSum);

public sealed record InventoryStatisticDto(
    int InventoriesCount,
    decimal CurrentSum,
    IReadOnlyList<InventoryGameShareDto> GameCount,
    IReadOnlyList<InventoryGameShareDto> GameSum);

public sealed record InventoryGameShareDto(
    string GameTitle,
    double Percentage,
    decimal Value);

public sealed record InventoryFilterDto(
    int? GameId,
    string? Filter,
    InventoryOrderName? OrderName,
    bool? IsAscending);
