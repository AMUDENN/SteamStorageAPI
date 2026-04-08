using SteamStorageAPI.Application.DTOs.ArchiveGroups;
using SteamStorageAPI.Domain.Entities;

namespace SteamStorageAPI.Application.Interfaces.Repositories;

public interface IArchiveGroupRepository
{
    Task<ArchiveGroup?> GetByIdAsync(int groupId, int userId, CancellationToken ct = default);

    Task<IReadOnlyList<ArchiveGroupDto>> GetAllAsync(
        int userId,
        GetArchiveGroupsFilterDto filter,
        CancellationToken ct = default);

    Task<ArchiveGroupsStatisticDto> GetStatisticAsync(int userId, CancellationToken ct = default);

    Task<int> GetCountAsync(int userId, CancellationToken ct = default);

    Task AddAsync(ArchiveGroup group, CancellationToken ct = default);

    Task UpdateAsync(ArchiveGroup group, CancellationToken ct = default);

    Task DeleteAsync(ArchiveGroup group, CancellationToken ct = default);
}
