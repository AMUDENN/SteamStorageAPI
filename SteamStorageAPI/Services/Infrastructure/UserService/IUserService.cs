using SteamStorageAPI.Models.DBEntities;

namespace SteamStorageAPI.Services.Infrastructure.UserService;

public interface IUserService
{
    Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}