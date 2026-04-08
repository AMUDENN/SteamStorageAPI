using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Application.DTOs.Inventory;
using SteamStorageAPI.Domain.Entities;

namespace SteamStorageAPI.Application.Interfaces.Repositories;

public interface IInventoryRepository
{
    Task<PagedResult<InventoryItemDto>> GetPagedAsync(
        int userId,
        InventoryFilterDto filter,
        PaginationDto pagination,
        CancellationToken ct = default);

    Task<InventoryStatisticDto> GetStatisticAsync(int userId, CancellationToken ct = default);

    Task<int> GetSavedCountAsync(int userId, CancellationToken ct = default);

    Task RefreshAsync(int userId, IEnumerable<Inventory> freshInventory, CancellationToken ct = default);
}
