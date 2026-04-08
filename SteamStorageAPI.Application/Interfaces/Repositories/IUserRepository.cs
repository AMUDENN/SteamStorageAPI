using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Application.DTOs.Users;
using SteamStorageAPI.Domain.Entities;

namespace SteamStorageAPI.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId, CancellationToken ct = default);

    Task<User?> GetBySteamIdAsync(long steamId, CancellationToken ct = default);

    Task<PagedResult<UserDto>> GetPagedAsync(PaginationDto pagination, CancellationToken ct = default);

    Task<UserDto?> GetDtoByIdAsync(int userId, CancellationToken ct = default);

    Task<int> GetCountAsync(CancellationToken ct = default);

    Task<bool> HasAdminAccessAsync(int userId, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);

    Task UpdateAsync(User user, CancellationToken ct = default);
}
