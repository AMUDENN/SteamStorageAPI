using SteamStorageAPI.DBEntities;

namespace SteamStorageAPI.Utilities.JWT
{
    public interface IJwtProvider
    {
        public string Generate(User tokenOwner);
    }
}
