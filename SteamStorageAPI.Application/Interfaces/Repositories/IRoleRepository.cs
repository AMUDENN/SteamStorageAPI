using SteamStorageAPI.Domain.Entities;
using SteamStorageAPI.Domain.Enums;

namespace SteamStorageAPI.Application.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int roleId, CancellationToken ct = default);
    Task<Role> GetByEnumAsync(UserRole role, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default);
}
