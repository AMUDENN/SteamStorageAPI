using SteamStorageAPI.Application.DTOs.Archives;
using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Domain.Entities;

namespace SteamStorageAPI.Application.Interfaces.Repositories;

public interface IArchiveRepository
{
    Task<Archive?> GetByIdAsync(int archiveId, int userId, CancellationToken ct = default);

    Task<PagedResult<ArchiveDto>> GetPagedAsync(
        int userId,
        ArchivesFilterDto filter,
        PaginationDto pagination,
        CancellationToken ct = default);

    Task<ArchiveStatisticDto> GetStatisticAsync(
        int userId,
        ArchivesFilterDto filter,
        CancellationToken ct = default);

    Task<int> GetCountAsync(
        int userId,
        ArchivesFilterDto filter,
        CancellationToken ct = default);

    Task AddAsync(Archive archive, CancellationToken ct = default);

    Task UpdateAsync(Archive archive, CancellationToken ct = default);

    Task DeleteAsync(Archive archive, CancellationToken ct = default);
}
