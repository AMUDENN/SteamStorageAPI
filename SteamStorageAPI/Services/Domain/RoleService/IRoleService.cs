using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Services.Domain.RoleService;

public interface IRoleService
{
    Task<RolesResponse> GetRolesAsync(
        CancellationToken cancellationToken = default);

    Task SetRoleAsync(
        User user,
        SetRoleRequest request,
        CancellationToken cancellationToken = default);
}