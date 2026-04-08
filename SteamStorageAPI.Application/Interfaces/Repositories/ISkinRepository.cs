using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Application.DTOs.Skins;
using SteamStorageAPI.Domain.Entities;

namespace SteamStorageAPI.Application.Interfaces.Repositories;

public interface ISkinRepository
{
    Task<Skin?> GetByIdAsync(int skinId, CancellationToken ct = default);

    Task<Skin?> GetByMarketHashNameAsync(string marketHashName, CancellationToken ct = default);

    Task<PagedResult<SkinDetailDto>> GetPagedAsync(
        int userId,
        SkinsFilterDto filter,
        PaginationDto pagination,
        CancellationToken ct = default);

    Task<SkinDynamicStatsDto> GetDynamicsAsync(
        GetSkinDynamicsDto dto,
        CancellationToken ct = default);

    Task<int> GetSteamSkinsCountAsync(int gameId, CancellationToken ct = default);

    Task<int> GetSavedSkinsCountAsync(CancellationToken ct = default);

    Task<bool> ExistsAsync(string marketHashName, CancellationToken ct = default);

    Task AddAsync(Skin skin, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<Skin> skins, CancellationToken ct = default);

    Task SetMarkedAsync(int skinId, int userId, CancellationToken ct = default);

    Task RemoveMarkedAsync(int skinId, int userId, CancellationToken ct = default);
}
