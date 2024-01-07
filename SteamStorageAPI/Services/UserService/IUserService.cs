using SteamStorageAPI.DBEntities;

namespace SteamStorageAPI.Services.UserService
{
    public interface IUserService
    {
        User? GetCurrentUser();
        User? FindUser(int Id);
        User? FindUser(long steamId);
    }
}
