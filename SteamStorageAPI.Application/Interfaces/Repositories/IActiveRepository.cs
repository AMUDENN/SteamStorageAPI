using SteamStorageAPI.Application.DTOs.Actives;
using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Domain.Entities;

namespace SteamStorageAPI.Application.Interfaces.Repositories;

public interface IActiveRepository
{
    Task<Active?> GetByIdAsync(int activeId, int userId, CancellationToken ct = default);

    Task<PagedResult<ActiveDto>> GetPagedAsync(
        int userId,
        ActivesFilterDto filter,
        PaginationDto pagination,
        CancellationToken ct = default);

    Task<ActiveStatisticDto> GetStatisticAsync(
        int userId,
        ActivesFilterDto filter,
        CancellationToken ct = default);

    Task<int> GetCountAsync(
        int userId,
        ActivesFilterDto filter,
        CancellationToken ct = default);

    Task AddAsync(Active active, CancellationToken ct = default);

    Task UpdateAsync(Active active, CancellationToken ct = default);

    Task DeleteAsync(Active active, CancellationToken ct = default);
}
