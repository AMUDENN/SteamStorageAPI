using SteamStorageAPI.DBEntities;

namespace SteamStorageAPI.Services.UserService
{
    public interface IUserService
    { 
        Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    }
}
