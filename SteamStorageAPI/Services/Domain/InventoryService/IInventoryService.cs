using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.DTOs.Enums;

namespace SteamStorageAPI.Services.Domain.InventoryService;

public interface IInventoryService
{
    Task<InventoriesResponse> GetInventoriesResponseAsync(
        IQueryable<Inventory> inventories,
        int pageNumber,
        int pageSize,
        User user,
        CancellationToken cancellationToken = default);

    Task<InventoriesStatisticResponse> GetInventoriesStatisticAsync(
        User user,
        int? gameId,
        string? filter,
        CancellationToken cancellationToken = default);

    IQueryable<Inventory> GetInventoryQuery(
        User user,
        int? gameId,
        string? filter);

    IQueryable<Inventory> ApplyOrder(
        IQueryable<Inventory> inventories,
        InventoryOrderName? orderName,
        bool? isAscending);

    Task RefreshInventoryAsync(
        User user,
        RefreshInventoryRequest request,
        CancellationToken cancellationToken = default);
}