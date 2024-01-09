using SteamStorageAPI.DBEntities;

namespace SteamStorageAPI.Services.UserService
{
    public interface IUserService
    {
        User? GetCurrentUser();
    }
}
