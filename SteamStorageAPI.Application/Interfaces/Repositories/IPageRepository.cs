using SteamStorageAPI.Domain.Entities;

namespace SteamStorageAPI.Application.Interfaces.Repositories;

public interface IPageRepository
{
    Task<Page?> GetByIdAsync(int pageId, CancellationToken ct = default);
    Task<IReadOnlyList<Page>> GetAllAsync(CancellationToken ct = default);
}
