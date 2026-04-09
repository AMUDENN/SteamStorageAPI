using SteamStorageAPI.Models.DBEntities;

namespace SteamStorageAPI.Services.Infrastructure.ContextUserService;

public interface IContextUserService
{
    Task<User?> GetContextUserAsync(CancellationToken cancellationToken = default);
}