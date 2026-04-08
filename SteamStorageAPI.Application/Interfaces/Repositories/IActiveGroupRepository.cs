using SteamStorageAPI.Application.DTOs.ActiveGroups;
using SteamStorageAPI.Domain.Entities;

namespace SteamStorageAPI.Application.Interfaces.Repositories;

public interface IActiveGroupRepository
{
    Task<ActiveGroup?> GetByIdAsync(int groupId, int userId, CancellationToken ct = default);

    Task<IReadOnlyList<ActiveGroupDto>> GetAllAsync(
        int userId,
        GetActiveGroupsFilterDto filter,
        CancellationToken ct = default);

    Task<ActiveGroupDynamicStatsDto> GetDynamicAsync(
        GetActiveGroupDynamicDto dto,
        CancellationToken ct = default);

    Task<ActiveGroupsStatisticDto> GetStatisticAsync(int userId, CancellationToken ct = default);

    Task<int> GetCountAsync(int userId, CancellationToken ct = default);

    Task AddAsync(ActiveGroup group, CancellationToken ct = default);

    Task UpdateAsync(ActiveGroup group, CancellationToken ct = default);

    Task DeleteAsync(ActiveGroup group, CancellationToken ct = default);
}
