using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Inventory;

namespace SteamStorageAPI.Models.DTOs;

public record InventoryResponse(
    int Id,
    BaseSkinResponse Skin,
    int Count,
    decimal CurrentPrice,
    decimal CurrentSum);

public record InventoriesResponse(
    int Count,
    int PagesCount,
    IEnumerable<InventoryResponse> Inventories);

public record InventoryGameCountResponse(
    string GameTitle,
    double Percentage,
    int Count);

public record InventoryGameSumResponse(
    string GameTitle,
    double Percentage,
    decimal Sum);

public record InventoriesStatisticResponse(
    int InventoriesCount,
    decimal CurrentSum,
    IEnumerable<InventoryGameCountResponse> GameCount,
    IEnumerable<InventoryGameSumResponse> GameSum);

public record InventoryPagesCountResponse(
    int Count);

public record SavedInventoriesCountResponse(
    int Count);

[Validator<GetInventoryRequestValidator>]
public record GetInventoryRequest(
    int? GameId,
    string? Filter,
    InventoryOrderName? OrderName,
    bool? IsAscending,
    int PageNumber,
    int PageSize);

[Validator<GetInventoriesStatisticRequestValidator>]
public record GetInventoriesStatisticRequest(
    int? GameId,
    string? Filter);

[Validator<GetInventoryPagesCountRequestValidator>]
public record GetInventoryPagesCountRequest(
    int? GameId,
    string? Filter,
    int PageSize);

[Validator<GetSavedInventoriesCountRequestValidator>]
public record GetSavedInventoriesCountRequest(
    int? GameId,
    string? Filter);

[Validator<RefreshInventoryRequestValidator>]
public record RefreshInventoryRequest(
    int GameId);